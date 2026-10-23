using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Roles.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Application.Common.Legacy;

/// <summary>
/// Reads a posted body into one of the original's data objects the way its DataContractJsonSerializer did, because
/// the saves act on what that produced:
/// <list type="bullet">
/// <item>no constructor or field initializer runs (instances start at CLR defaults);</item>
/// <item>every data member is required at every depth, a duplicated member fails, unknown members are ignored;</item>
/// <item>members are applied in posted order through the original's setters, which dirty the object, truncate
/// captions and (for some types) stamp <c>LastModified</c> with the server clock.</item>
/// </list>
/// Every failure is a <see cref="LegacyBodyException"/> (the Request Error page).
/// </summary>
public sealed class DcjsReader(Func<DateTime> localNow)
{
    /// <summary>The original setter of a member.</summary>
    private enum Setter
    {
        /// <summary>A plain store.</summary>
        Plain,
        /// <summary><c>if (_x != value) { _x = value; IsDirty = true; }</c>; also non-Guid compares and settable lists (reference compare).</summary>
        Changed,
        /// <summary><c>if (!_x.HasValue || _x.Value.CompareTo(value) != 0)</c>: dirties whenever the field had no value.</summary>
        HasValue,
        /// <summary>A private no-op setter: the value is read and dropped.</summary>
        Ignored
    }

    /// <summary>The IsDirty setter: D0 no stamp; D1 stamps on a false-to-true change; D2 stamps on every change.</summary>
    private enum Dirty { NoStamp, StampOnSet, StampOnChange }

    private sealed record MemberRule(Setter Setter, int TruncateAbove = 0, int KeepLength = 0);

    private sealed record ListRule(bool NullItemFails, Func<object, object?>? DedupKey);

    private sealed record TypeRule(Dirty Dirty, Dictionary<string, MemberRule> Members, string[] GetOnlyLists, bool NullGetOnlyListFails = false);

    private static readonly MemberRule Plain = new(Setter.Plain);
    private static readonly MemberRule Changed = new(Setter.Changed);
    private static readonly MemberRule HasValueRule = new(Setter.HasValue);
    private static readonly MemberRule Caption = new(Setter.Changed, 100, 99);

    private static Dictionary<string, MemberRule> Rules(string changed, string hasValue, string plain = "", params (string Name, MemberRule Rule)[] other)
    {
        var rules = new Dictionary<string, MemberRule>(StringComparer.Ordinal);
        foreach (var name in Split(changed))
            rules[name] = Changed;
        foreach (var name in Split(hasValue))
            rules[name] = HasValueRule;
        foreach (var name in Split(plain))
            rules[name] = Plain;
        foreach (var (name, rule) in other)
            rules[name] = rule;
        return rules;

        static string[] Split(string names) => names.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    // Spec M6-M 0.4.4. Guid members compared with CompareTo behave as "Changed".
    private static readonly Dictionary<Type, TypeRule> TypeRules = new()
    {
        [typeof(AssetTypeContract)] = new(Dirty.NoStamp,
            Rules("AllowAssets Created Deleted LastModified CreatedBy Id", "DefaultInstanceViewId DefaultViewId DeletedBy ImageId LastModifiedBy ParentId",
                "DefinitionLabel InstanceLabel Properties", ("Name", Caption), ("Pluralization", Caption)),
            ["PropertyMembers"]),
        [typeof(AssetTypePropertyRelationContract)] = new(Dirty.NoStamp,
            Rules("AssetTypeId Created CreatedBy Id IsInherited IsInheritedValue IsInstance PropertyId", "Deleted DeletedBy"), []),
        [typeof(PropertyContract)] = new(Dirty.StampOnSet,
            Rules("AllowMultiValue Approved AssetTypeIsInstance Created CreatedBy DataType DefaultValue Deleted Description Id IsInherited IsInheritedValue "
                + "IsInstance IsOrdered IsReadOnly IsRelationship IsRequired IsSystem LastModified Precision SystemType",
                "ApprovedBy AssetTypeId DeletedBy LastModifiedBy PickListId RoleId", "AllowNewValues Properties Triggers",
                ("DisplayValue", Caption), ("Name", Caption), ("Plural", Caption), ("Singular", Caption), ("UsesRole", new MemberRule(Setter.Ignored))),
            []),
        [typeof(PickListContract)] = new(Dirty.StampOnSet,
            Rules("AllowNewValues Created CreatedBy Dependencies Description Id IsMemberList PickListValues ServiceUrl",
                "Approved ApprovedBy Deleted DeletedBy LastModified LastModifiedBy", "Properties",
                ("DisplayValue", Caption), ("Name", Caption), ("ServiceMethod", new MemberRule(Setter.Changed, 255, 254)),
                ("ServicePassword", new MemberRule(Setter.Changed, 50, 49)), ("ServiceUsername", new MemberRule(Setter.Changed, 50, 49))),
            []),
        [typeof(PickListValueContract)] = new(Dirty.StampOnChange,
            Rules("Approved BGColor Created CreatedBy Deleted Id LastModified PickListId", "ApprovedBy DeletedBy LastModifiedBy Order", "",
                ("DisplayValue", new MemberRule(Setter.Changed, 255, 254)), ("Value", new MemberRule(Setter.Changed, 255, 254))),
            []),
        [typeof(PropertyGroupContract)] = new(Dirty.NoStamp,
            Rules("AssetTypeId Created CreatedBy Id IsInstance", "Deleted DeletedBy LastModified LastModifiedBy", "Properties",
                ("DisplayValue", Caption), ("Name", Caption)),
            ["PropertyMembers"]),
        [typeof(PropertyGroupPropertyRelationContract)] = new(Dirty.NoStamp,
            Rules("Created CreatedBy DefaultValue Id IsReadOnly IsRequired Order PropertyGroupId PropertyId", "Deleted DeletedBy"), []),
        [typeof(ViewContract)] = new(Dirty.StampOnSet,
            Rules("AssetTypeId ConfirmationLabel Created CreatedBy Description DriverCaption Id IsCreation IsInstance IsReadOnly IsStandAlone NewItemCaption",
                "Deleted DeletedBy LastModified LastModifiedBy", "AllowCloning AllowNewValues FilterLogic Filters IsAutoApprove PropertyGroups",
                ("DisplayValue", Caption), ("Name", Caption)),
            ["PropertyGroupMembers"], NullGetOnlyListFails: true),
        [typeof(ViewPropertyGroupRelationContract)] = new(Dirty.NoStamp,
            Rules("Created CreatedBy Deleted Id Order PropertyGroupId ViewId", "DeletedBy"), [])
    };

    // The lists' Add: relation lists skip a second item for the same target and fail on a null item.
    private static readonly Dictionary<Type, ListRule> ListRules = new()
    {
        [typeof(PropertyGroupPropertyRelationContract)] = new(true, item => ((PropertyGroupPropertyRelationContract)item).PropertyId),
        [typeof(ViewPropertyGroupRelationContract)] = new(true, item => ((ViewPropertyGroupRelationContract)item).PropertyGroupId)
    };

    private static readonly ListRule DefaultList = new(false, null);

    public T Read<T>(JsonElement element) where T : class => (T)ReadObject(typeof(T), element);

    /// <summary>
    /// A bare request body: the host's early answers first (no Content-Type, malformed JSON), an empty or literal null
    /// body as a null parameter, a binding failure as the Request Error page.
    /// </summary>
    public async Task<(T? Body, IActionResult? Error)> ReadBodyAsync<T>(HttpRequest request, CancellationToken cancellationToken) where T : class
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(request, cancellationToken);
        if (error is not null || root is null)
            return (null, error);
        try
        {
            return (Read<T>(root.Value), null);
        }
        catch (LegacyBodyException e)
        {
            return (null, e.ToResult());
        }
    }

    private object ReadObject(Type type, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new LegacyBodyException("Not an object.");
        var members = DataMembers(type);
        var rule = TypeRules.GetValueOrDefault(type);
        var instance = RuntimeHelpers.GetUninitializedObject(type);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in element.EnumerateObject())
        {
            if (!members.TryGetValue(member.Name, out var property))
                continue;
            if (!seen.Add(member.Name))
                throw new LegacyBodyException($"The data member '{member.Name}' was found more than once.");
            Apply(instance, rule, member.Name, property, member.Value);
        }

        if (seen.Count != members.Count)
            throw new LegacyBodyException("A required data member was not found.");
        return instance;
    }

    private void Apply(object instance, TypeRule? rule, string name, PropertyInfo property, JsonElement value)
    {
        if (rule is not null && name == "IsDirty")
        {
            SetDirty(instance, rule, ReadValue(typeof(bool), value) is true);
            return;
        }

        if (rule is not null && rule.GetOnlyLists.Contains(name))
        {
            // The serializer calls the getter (creating the list) and adds each item: no setter runs.
            if (value.ValueKind == JsonValueKind.Null && rule.NullGetOnlyListFails)
                throw new LegacyBodyException("The serializer assigned the null list to another member.");
            property.SetValue(instance, value.ValueKind == JsonValueKind.Null ? NewList(property.PropertyType) : ReadValue(property.PropertyType, value));
            return;
        }

        var read = ReadValue(property.PropertyType, value);
        var member = rule?.Members.GetValueOrDefault(name) ?? Plain;
        if (member.TruncateAbove > 0 && read is string text && text.Length > member.TruncateAbove)
            read = text[..member.KeepLength];

        switch (member.Setter)
        {
            case Setter.Ignored:
                return;
            case Setter.Plain:
                property.SetValue(instance, read);
                return;
            case Setter.Changed:
                if (Equals(property.GetValue(instance), read))
                    return;
                break;
            case Setter.HasValue:
                var current = property.GetValue(instance);
                if (current is not null && Equals(current, read))
                    return;
                break;
        }
        property.SetValue(instance, read);
        if (rule is not null)
            SetDirty(instance, rule, true);
    }

    /// <summary>The original IsDirty setter (spec 0.4.3); OnModified writes LastModified unless the object is new.</summary>
    private void SetDirty(object instance, TypeRule rule, bool value)
    {
        var type = instance.GetType();
        var isDirty = type.GetProperty("IsDirty")!;
        if ((bool)isDirty.GetValue(instance)! == value)
            return;
        isDirty.SetValue(instance, value);
        if (rule.Dirty == Dirty.NoStamp || (rule.Dirty == Dirty.StampOnSet && !value))
            return;
        if (!(bool)type.GetProperty("IsNew")!.GetValue(instance)!)
            type.GetProperty("LastModified")!.SetValue(instance, localNow());
    }

    private object? ReadValue(Type type, JsonElement value)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (value.ValueKind == JsonValueKind.Null)
        {
            if (type.IsValueType && underlying is null)
                throw new LegacyBodyException($"ValueType '{type}' cannot be null.");
            return null;
        }

        var target = underlying ?? type;
        if (target == typeof(string))
            return LegacyJsonBody.Text(value);
        if (target == typeof(Guid))
            return RoleBodies.Guid(value);
        if (target == typeof(bool))
            return LegacyJsonBody.Bool(value);
        if (target == typeof(int))
            return LegacyJsonBody.Int(value);
        if (target == typeof(short))
        {
            var number = LegacyJsonBody.Int(value);
            return number is < short.MinValue or > short.MaxValue
                ? throw new LegacyBodyException(string.Create(CultureInfo.InvariantCulture, $"The value '{number}' cannot be parsed as the type 'Int16'."))
                : (short)number;
        }
        if (target == typeof(DateTime))
            return RoleBodies.Date(value);
        if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(List<>))
            return ReadList(target, value);
        return ReadObject(target, value);
    }

    private IList ReadList(Type listType, JsonElement value)
    {
        var list = NewList(listType);
        if (value.ValueKind == JsonValueKind.Object)
            return list;
        if (value.ValueKind != JsonValueKind.Array)
            throw new LegacyBodyException("Not a list.");
        var itemType = listType.GetGenericArguments()[0];
        var rule = ListRules.GetValueOrDefault(itemType) ?? DefaultList;
        var keys = new HashSet<object>();
        foreach (var element in value.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                if (rule.NullItemFails)
                    throw new LegacyBodyException("Object reference not set to an instance of an object.");
                list.Add(null);
                continue;
            }
            var item = ReadValue(itemType, element)!;
            if (rule.DedupKey is not null && !keys.Add(rule.DedupKey(item)!))
                continue;
            list.Add(item);
        }
        return list;
    }

    private static IList NewList(Type listType) => (IList)Activator.CreateInstance(listType)!;

    private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> MemberCache = [];

    private static Dictionary<string, PropertyInfo> DataMembers(Type type)
    {
        lock (MemberCache)
        {
            if (!MemberCache.TryGetValue(type, out var members))
            {
                members = type.GetProperties()
                    .Select(p => (Property: p, Member: p.GetCustomAttribute<DataMemberAttribute>()))
                    .Where(p => p.Member is not null)
                    .ToDictionary(p => p.Member!.Name ?? p.Property.Name, p => p.Property, StringComparer.Ordinal);
                MemberCache[type] = members;
            }
            return members;
        }
    }
}
