using System;
using System.Collections.Generic;
using System.Formats.Nrbf;
using System.IO;

namespace Xdb.Api.Application.Views.Legacy;

/// <summary>
/// Reads the original's cached client views (BinaryFormatter / MS-NRBF blobs in ViewClients) without loading types:
/// members are bound by their auto-property backing-field names, assembly names and versions are ignored (rows were
/// written by more than one build), and missing members keep their defaults.
/// </summary>
public static class NrbfViewClientReader
{
    public static ViewClientContract Read(byte[] data)
    {
        var root = (ClassRecord)NrbfDecoder.Decode(new MemoryStream(data));
        var client = new ViewClientContract
        {
            Id = GuidOf(Raw(root, "Id")) ?? Guid.Empty,
            Name = String(root, "Name"),
            AssetTypeId = GuidOf(Raw(root, "AssetTypeId")) ?? Guid.Empty,
            IsInstance = Bool(root, "IsInstance"),
            IsReadOnly = Bool(root, "IsReadOnly"),
            IsCreation = Bool(root, "IsCreation"),
            AllowNewValues = Bool(root, "AllowNewValues"),
            DriverCaption = String(root, "DriverCaption"),
            NewItemCaption = String(root, "NewItemCaption"),
            IconId = Raw(root, "iconId") as string,
            LastModified = String(root, "lastModified"),
            PrefetchAssets = Bool(root, "prefetchAssets")
        };

        foreach (var group in ListItems(Raw(root, "PropertyGroups")))
        {
            var groupClient = new PropertyGroupClientContract
            {
                Id = GuidOf(Raw(group, "Id")) ?? Guid.Empty,
                Name = String(group, "Name")
            };
            foreach (var property in ListItems(Raw(group, "Properties")))
                groupClient.Properties.Add(Property(property));
            client.PropertyGroups.Add(groupClient);
        }

        if (Raw(root, "filters") is ClassRecord dictionary && dictionary.HasMember("KeyValuePairs")
            && dictionary.GetRawValue("KeyValuePairs") is SZArrayRecord<SerializationRecord> pairs)
        {
            foreach (var pair in pairs.GetArray())
            {
                if (pair is ClassRecord kv)
                    client.Filters.Add(GuidOf(kv.GetRawValue("key")) ?? Guid.Empty, kv.GetRawValue("value") as string ?? string.Empty);
            }
        }

        return client;
    }

    private static PropertyClientContract Property(ClassRecord record)
    {
        var property = new PropertyClientContract
        {
            Id = GuidOf(Raw(record, "Id")) ?? Guid.Empty,
            Name = String(record, "Name"),
            Description = String(record, "Description"),
            IsReadOnly = Bool(record, "IsReadOnly"),
            DataType = EnumOf(Raw(record, "DataType")),
            SystemType = EnumOf(Raw(record, "SystemType")),
            PickListId = GuidOf(Raw(record, "PickListId")),
            Precision = Raw(record, "Precision") is short precision ? precision : null,
            AllowMultiValue = Bool(record, "AllowMultiValue"),
            AllowNewValues = Bool(record, "AllowNewValues"),
            IsInherited = Bool(record, "IsInherited"),
            IsInheritedValue = Bool(record, "IsInheritedValue"),
            IsInstance = Bool(record, "IsInstance"),
            IsSystem = Bool(record, "IsSystem"),
            IsOrdered = Bool(record, "IsOrdered"),
            IsRequired = Bool(record, "IsRequired"),
            DefaultValue = String(record, "DefaultValue"),
            IsRelationship = Bool(record, "IsRelationship"),
            AssetTypeId = GuidOf(Raw(record, "AssetTypeId")),
            AssetTypeIsInstance = Raw(record, "AssetTypeIsInstance") is bool isInstance ? isInstance : null,
            RoleId = GuidOf(Raw(record, "RoleId")),
            UsesRole = Bool(record, "UsesRole")
        };
        foreach (var trigger in ListItems(Raw(record, "Triggers")))
        {
            property.Triggers.Add(new PropertyTriggerContract
            {
                TriggerType = EnumOf(Raw(trigger, "TriggerType")),
                ParentPropertyId = GuidOf(Raw(trigger, "ParentPropertyId")) ?? Guid.Empty,
                TargetPropertyId = GuidOf(Raw(trigger, "TargetPropertyId")) ?? Guid.Empty,
                SourcePropertyId = GuidOf(Raw(trigger, "SourcePropertyId")) ?? Guid.Empty
            });
        }
        return property;
    }

    /// <summary>An auto-property's backing field (or a plain field of that name), or null when absent.</summary>
    private static object? Raw(ClassRecord record, string name)
    {
        var field = $"<{name}>k__BackingField";
        if (record.HasMember(field))
            return record.GetRawValue(field);
        return record.HasMember(name) ? record.GetRawValue(name) : null;
    }

    private static string String(ClassRecord record, string name) => Raw(record, name) as string ?? string.Empty;

    private static bool Bool(ClassRecord record, string name) => Raw(record, name) is true;

    private static Guid? GuidOf(object? value) => value is ClassRecord guid
        ? new Guid((int)guid.GetRawValue("_a")!, (short)guid.GetRawValue("_b")!, (short)guid.GetRawValue("_c")!,
            (byte)guid.GetRawValue("_d")!, (byte)guid.GetRawValue("_e")!, (byte)guid.GetRawValue("_f")!, (byte)guid.GetRawValue("_g")!,
            (byte)guid.GetRawValue("_h")!, (byte)guid.GetRawValue("_i")!, (byte)guid.GetRawValue("_j")!, (byte)guid.GetRawValue("_k")!)
        : null;

    private static int EnumOf(object? value) => value switch
    {
        ClassRecord e when e.HasMember("value__") => Convert.ToInt32(e.GetRawValue("value__"), System.Globalization.CultureInfo.InvariantCulture),
        int i => i,
        _ => 0
    };

    /// <summary>A List&lt;T&gt;'s items: the first _size entries of _items.</summary>
    private static IEnumerable<ClassRecord> ListItems(object? value)
    {
        if (value is not ClassRecord list || !list.HasMember("_items"))
            yield break;
        var size = list.HasMember("_size") && list.GetRawValue("_size") is int s ? s : 0;
        if (list.GetRawValue("_items") is not SZArrayRecord<SerializationRecord> items)
            yield break;
        var array = items.GetArray();
        for (var i = 0; i < size && i < array.Length; i++)
        {
            if (array[i] is ClassRecord item)
                yield return item;
        }
    }
}
