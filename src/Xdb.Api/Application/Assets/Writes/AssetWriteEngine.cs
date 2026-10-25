using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Writes;

/// <summary>Fixed ids the original's narratives and relation rules test (its PropertyIds/AssetTypeIds/PickListValues constants).</summary>
public static class NarrativeIds
{
    public static readonly Guid AssetParentProperty = new("9186C777-C16D-412D-A144-1EEC68AD55E6");
    public static readonly Guid StatusProperty = new("2B4092D8-CA5D-4546-94E3-818A33F127FC");
    public static readonly Guid AssigneeProperty = new("BA357EED-63DE-434F-9882-A9EA27A60A7B");
    public static readonly Guid HardwareType = new("C2FC2CDF-9FAD-4A7C-8518-EEDDC3E3227D");
    public static readonly Guid SoftwareType = new("473F1298-8A9F-4C3D-8AA5-358A27D0735D");
    public static readonly Guid ReleaseType = new("0F0631F7-334F-4218-8F3E-B1F1EC68A71C");
    public static readonly Guid StatusAssigned = new("EB3D0469-D885-4483-A74D-758B0448F27A");
    public static readonly Guid StatusInStock = new("9D9960D4-F326-4EEC-8A77-D0C7F0EDD591");
    public static readonly Guid StatusDisposed = new("7AB6B6F1-53F4-4C52-A415-C3DED42DE904");
    public static readonly Guid StatusLost = new("A61506F8-F239-402E-9AE5-ABA10409F75D");
}

/// <summary>The original's relation types (EAssetRelationType).</summary>
public static class RelationTypes
{
    public const byte ParentChild = 0;
    public const byte Dependency = 1;
}

/// <summary>
/// SqlDatabaseLayer's gen-table work after asset writes (spec-regenerate 3): insert into the type stack's tables, refresh
/// the tables already holding the asset, delete from every gen table. Run in the background; a missing procedure
/// stops the insert there (the original then lost its worker process; the port logs).
/// </summary>
public sealed class GenTableRefresher(IAssetWriteProcedures writes, IMetadataProcedures metadata)
{
    public async Task InsertAsync(Guid assetId, bool isInstance, Guid assetTypeId, CancellationToken cancellationToken)
    {
        foreach (var typeId in await StackAsync(assetTypeId, cancellationToken))
        {
            // A type without an instance label has no instance gen tables (an ancestor of an instance type may not);
            // the original stopped at its missing procedure and never reached the asset's own type.
            if (isInstance && string.IsNullOrEmpty((await metadata.GetAssetTypeAsync(typeId, cancellationToken))?.InstanceLabel))
                continue;

            await writes.ExecuteGenProcedureAsync(GeneratedObjectNames.UpdateProcedure(GeneratedObjectNames.Table(typeId, isInstance, isRaw: true)), assetId, cancellationToken);
            await writes.ExecuteGenProcedureAsync(GeneratedObjectNames.UpdateProcedure(GeneratedObjectNames.Table(typeId, isInstance, isRaw: false)), assetId, cancellationToken);
        }
    }

    public async Task UpdateAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var holding = await writes.GetGenTablesHoldingAsync(await writes.GetGenTableNamesAsync(cancellationToken), assetId, cancellationToken);
        var batch = new StringBuilder();
        foreach (var table in holding)
            batch.Append($"EXEC spr_{table}_Update '{assetId}'").Append("\r\n");
        if (batch.Length > 0)
            await writes.ExecuteBatchAsync(batch.ToString(), cancellationToken);
    }

    public async Task DeleteAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var batch = new StringBuilder();
        foreach (var table in await writes.GetGenTableNamesAsync(cancellationToken))
            batch.Append($"DELETE FROM [{table}] WHERE [AssetId] = '{assetId}'").Append("\r\n");
        await writes.ExecuteBatchAsync(batch.ToString(), cancellationToken);
    }

    /// <summary>AssetTypeLayer.GetStack: ancestors nearest first, then the descendants by name and the type itself.</summary>
    private async Task<List<Guid>> StackAsync(Guid assetTypeId, CancellationToken cancellationToken)
    {
        var stack = new List<Guid>();
        var parent = await metadata.GetParentAssetTypeIdAsync(assetTypeId, cancellationToken);
        while (parent.HasValue)
        {
            stack.Add(parent.Value);
            parent = await metadata.GetParentAssetTypeIdAsync(parent.Value, cancellationToken);
        }
        foreach (var typeId in new List<Guid>(await metadata.GetDescendantAssetTypeIdsAsync(assetTypeId, cancellationToken)) { assetTypeId })
            if (!stack.Contains(typeId))
                stack.Add(typeId);
        return stack;
    }
}

/// <summary>AssetLayer's save path (spec M6 2.1, 2.4): load with permissions, validate, upsert with relations, mark updated.</summary>
public sealed class AssetWriter(IAssetWriteProcedures writes, IAssetProcedures assets, IMetadataProcedures metadata, IBackgroundWorkQueue background)
{
    private const int NameLimit = 255;
    private const int DisplayValueLimit = 100;

    /// <summary>The Asset name setter: longer than 255 characters keeps 254.</summary>
    public static string? Name(string? name) => name is { Length: > NameLimit } ? name[..(NameLimit - 1)] : name;

    /// <summary>The Asset display value setter: longer than 100 characters keeps 99.</summary>
    public static string? DisplayValue(string? text) => text is { Length: > DisplayValueLimit } ? text[..(DisplayValueLimit - 1)] : text;

    /// <summary>AssetLayer.Asset_Get: the row (deleted ones included), editable and deletable only for an administrator.</summary>
    public async Task<AssetContract?> GetAsync(Guid assetId, Guid userId, CancellationToken cancellationToken)
    {
        var row = await assets.GetAssetAsync(assetId, cancellationToken);
        if (row is null)
            return null;
        var isAdmin = await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken);
        return new AssetContract
        {
            Id = assetId, AssetTypeId = row.AssetTypeId, Name = row.Name, DisplayValue = row.DisplayValue ?? string.Empty, Description = row.Description ?? string.Empty,
            IsInstance = row.IsInstance ?? false, InstanceOfId = row.InstanceOfId, Created = row.Created, Approved = row.Approved, LastModified = row.LastModified,
            CreatedBy = row.CreatedBy, ApprovedBy = row.ApprovedBy, LastModifiedBy = row.LastModifiedBy, Deleted = row.Deleted, DeletedBy = row.DeletedBy,
            AllowDelete = isAdmin, AllowEdit = isAdmin
        };
    }

    /// <summary>AssetLayer.Save: validate, then (when dirty) spr_Asset_Save and each relation not already live.</summary>
    public async Task<bool> SaveAsync(AssetContract asset, IReadOnlyList<RelationToSave> relations, CancellationToken cancellationToken)
    {
        await ValidateAsync(asset, cancellationToken);
        if (asset.IsDirty)
        {
            await writes.SaveAssetAsync(asset, cancellationToken);
            foreach (var relation in relations)
            {
                if (!await writes.RelationExistsAsync(relation.From, relation.To, relation.Type, cancellationToken))
                    await writes.SaveRelationAsync(relation.Id, relation.From, relation.To, relation.Type, relation.Created, relation.Approved, relation.CreatedBy,
                        relation.ApprovedBy, cancellationToken);
            }
        }
        asset.IsNew = false;
        asset.IsDirty = false;
        return true;
    }

    public async Task ValidateAsync(AssetContract asset, CancellationToken cancellationToken)
    {
        if (asset.Id == Guid.Empty)
            throw new LegacyLogicalException("Id cannot be null");
        if (asset.IsInstance)
        {
            if (!asset.InstanceOfId.HasValue)
                throw new LegacyLogicalException("InstanceOfId cannot be null for asset objects that are instances");
            if (!await writes.IsLiveAssetAsync(asset.InstanceOfId.Value, cancellationToken))
                throw new LegacyLogicalException("InstanceOfId is invalid");
        }
        if (string.IsNullOrEmpty(asset.Name))
            throw new LegacyLogicalException("Name must be defined");
        if (asset.AssetTypeId == Guid.Empty)
            throw new LegacyLogicalException("Asset Type must be defined");
        if (asset.CreatedBy == Guid.Empty)
            throw new LegacyLogicalException("'Created By' cannot be null");
        if (asset.IsDirty && !asset.IsNew && !asset.LastModifiedBy.HasValue)
            throw new LegacyLogicalException("'Last Modified By' cannot be null");
        if (!asset.AllowDelete && asset.Deleted.HasValue)
            throw new LegacyLogicalException("User permissions state you are not allowed to delete this asset.");
        if (!await writes.IsLiveAssetTypeAsync(asset.AssetTypeId, cancellationToken))
            throw new LegacyLogicalException("Specified asset type id does not exist");
        if (!await metadata.IsLiveMemberIdAsync(asset.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
    }

    /// <summary>AssetLayer.MarkAsUpdated: the cache and LastModified batch, then the gen-table refresh in the background.</summary>
    public async Task MarkAsUpdatedAsync(Guid assetId, Guid userId, CancellationToken cancellationToken)
    {
        await writes.MarkAsUpdatedAsync(assetId, userId, cancellationToken);
        background.Enqueue($"gen tables update {assetId}", (services, ct) => services.GetRequiredService<GenTableRefresher>().UpdateAsync(assetId, ct));
    }
}

/// <summary>A relation the asset save inserts (AssetAssetRelation as built by Asset_Create).</summary>
public sealed record RelationToSave(Guid Id, Guid From, Guid To, byte Type, DateTime Created, DateTime? Approved, Guid CreatedBy, Guid? ApprovedBy);

/// <summary>A property-value submittal (PropertyValueSubmittal): one review of a set of values for one asset.</summary>
public sealed class Submittal(Guid id, Guid assetId, DateTime created, Guid createdBy)
{
    public Guid Id { get; } = id;
    public Guid AssetId { get; } = assetId;
    public DateTime Created { get; } = created;
    public Guid CreatedBy { get; } = createdBy;
    public DateTime? Reviewed { get; set; }
    public Guid? ReviewedBy { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDirty { get; set; } = true;
    public List<SubmittalValue> Values { get; } = [];
}

/// <summary>
/// PropertyValueSubmittalLayer.Save of the deployed release (spec M6 2.3): there is no pending path, every save is
/// approved by the acting member. The first pass stores the submittal and values; Approve stamps them, runs the same
/// save again (the second pass), then applies relations and renames and marks the asset updated. No transaction.
/// </summary>
public sealed class SubmittalEngine(
    IAssetWriteProcedures writes, IAssetProcedures reads, IMetadataProcedures metadata, AssetWriter assets, IBackgroundWorkQueue background, IDateTimeProvider clock)
{
    /// <summary>The original parsed values in the server's culture (en-US).</summary>
    private static readonly System.Globalization.CultureInfo EnUs = System.Globalization.CultureInfo.GetCultureInfo("en-US");

    private DateTime Now() => clock.UtcNow().ToLocalTime();

    public async Task<bool> SaveAsync(Submittal submittal, bool isAutoApprove, Guid userId, CancellationToken cancellationToken)
    {
        if (submittal.AssetId == Guid.Empty)
            throw new LegacyLogicalException("AssetId was not provided for the Submittal Group");
        var isNew = submittal.IsNew;
        if (submittal.IsDirty)
        {
            await writes.SaveSubmittalAsync(submittal.Id, submittal.AssetId, submittal.Created, submittal.Reviewed, submittal.CreatedBy, submittal.ReviewedBy,
                cancellationToken);
            submittal.IsNew = false;
            submittal.IsDirty = false;
        }
        if (!await SaveValuesAsync(submittal.Values, userId, cancellationToken))
            return false;

        if (isAutoApprove)
        {
            await ApproveAsync(submittal, userId, cancellationToken);
            var submittalId = submittal.Id;
            background.Enqueue($"narrative {submittalId}", (services, ct) => services.GetRequiredService<NarrativeWriter>().WriteAsync(submittalId, ct));
        }
        if (!isNew)
        {
            var assetId = submittal.AssetId;
            background.Enqueue($"mark updated {assetId}", (services, ct) => services.GetRequiredService<AssetWriter>().MarkAsUpdatedAsync(assetId, userId, ct));
        }
        return true;
    }

    /// <summary>PropertyLayer.GetDictionary: the properties of the given ids (deleted ones included).</summary>
    public async Task<Dictionary<Guid, PropertyContract>> PropertiesAsync(IEnumerable<Guid> propertyIds, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        foreach (var id in propertyIds)
            if (!ids.Contains(id))
                ids.Add(id);
        return (await metadata.GetPropertiesAsync(ids, cancellationToken)).ToDictionary(row => row.Id, MetadataLoader.Property);
    }

    /// <summary>PropertyValueLayer.PropertyValueList_Save (spec 2.3.2-2.3.3).</summary>
    private async Task<bool> SaveValuesAsync(List<SubmittalValue> values, Guid userId, CancellationToken cancellationToken)
    {
        var properties = await PropertiesAsync(values.Select(v => v.PropertyId), cancellationToken);

        var seen = new HashSet<Guid>();
        foreach (var value in values)
        {
            var property = properties[value.PropertyId];
            if (!seen.Add(value.PropertyId) && !property.AllowMultiValue)
                throw new LegacyLogicalException($"Multiple values specified for {property.Name}, but does not allow multi-values");
            if (!await IsValidAsync(property, value, cancellationToken))
                return false;
        }

        foreach (var value in values)
        {
            if (!value.IsDirty)
                continue;
            var property = properties[value.PropertyId];
            var caseSensitive = property.IsSystem && property.SystemType == (int)SystemType.AssetName;
            var deleteExisting = !property.AllowMultiValue;
            // An identical live value is not written again; the list still marks the value saved (not new) afterwards.
            if (!(value.IsNew && await writes.ValueExistsAsync(value.PropertyId, value.AssetId, value.Value, caseSensitive, cancellationToken)))
            {
                if (deleteExisting)
                    await writes.DeleteValuesForAssetAsync(value.PropertyId, value.CreatedBy, value.AssetId, cancellationToken);
                await writes.SaveValueAsync(value, cancellationToken);
            }
            value.IsNew = false;
            value.IsDirty = false;
        }

        foreach (var value in values)
        {
            var property = properties[value.PropertyId];
            if (!property.IsSystem)
                continue;
            var asset = await assets.GetAsync(value.AssetId, userId, cancellationToken);
            if (property.SystemType == (int)SystemType.AssetName)
            {
                if (asset is null)
                    continue;
                asset.Name = AssetWriter.Name(value.Value);
            }
            else if (property.SystemType == (int)SystemType.AssetType)
            {
                if (asset is null)
                    continue;
                var newTypeId = new Guid(value.Value!);
                await writes.ChangeInstancesTypeAsync(newTypeId, asset.Id, cancellationToken);
                asset.AssetTypeId = new Guid(value.Value!);
            }
            else if (property.SystemType == (int)SystemType.Description)
            {
                if (asset is null)
                    continue;
                asset.Description = value.Value;
            }
            else
            {
                continue;
            }
            asset.LastModified = Now();
            asset.LastModifiedBy = value.CreatedBy;
            asset.IsDirty = true;
            await assets.SaveAsync(asset, [], cancellationToken);
        }
        return true;
    }

    /// <summary>PropertyValueLayer.PropertyValueIsValid: logical failures throw; an invalid asset type value returns false.</summary>
    private async Task<bool> IsValidAsync(PropertyContract property, SubmittalValue value, CancellationToken cancellationToken)
    {
        if (value.Id == Guid.Empty)
            throw new LegacyLogicalException("Id cannot be null");
        if (value.PropertyId == Guid.Empty)
            throw new LegacyLogicalException("PropertyId cannot be null");
        if (value.SubmittalGroupId == Guid.Empty)
            throw new LegacyLogicalException("SubmittalGroupId cannot be null");
        if (value.AssetId == Guid.Empty)
            throw new LegacyLogicalException("AssetId is missing");
        if (value.CreatedBy == Guid.Empty)
            throw new LegacyLogicalException("'Created By' cannot be null");
        if (!await metadata.IsLiveMemberIdAsync(value.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
        if (string.IsNullOrEmpty(value.Value))
            return true;

        if (property.IsSystem)
        {
            if (property.SystemType == (int)SystemType.AssetType)
                return Guid.TryParse(value.Value, out var typeId) && await writes.IsLiveAssetTypeAsync(typeId, cancellationToken);
            return true;
        }

        var typeName = ((LegacyDataType)property.DataType).ToString();
        var notValid = $"For property '{property.Name}', data type is '{typeName}' but provided value is not.  Value provided was '{value.Value}'.";
        switch ((LegacyDataType)property.DataType)
        {
            case LegacyDataType.Date:
            case LegacyDataType.DateTime:
                if (!DateTime.TryParse(value.Value, EnUs, System.Globalization.DateTimeStyles.None, out _))
                    throw new LegacyLogicalException(notValid);
                break;
            case LegacyDataType.Float:
                if (!float.TryParse(value.Value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, EnUs, out _))
                    throw new LegacyLogicalException(notValid);
                break;
            case LegacyDataType.Int:
                if (!int.TryParse(value.Value, System.Globalization.NumberStyles.Integer, EnUs, out _))
                    throw new LegacyLogicalException(notValid);
                break;
            case LegacyDataType.URL:
                var url = await reads.GetUrlAsync(new Guid(value.Value), cancellationToken);
                if (url is not null && !Uri.TryCreate(url.Url, UriKind.Absolute, out _))
                    throw new LegacyLogicalException(notValid);
                break;
            case LegacyDataType.User:
                if (!Guid.TryParse(value.Value, out var memberId))
                    throw new LegacyLogicalException(notValid);
                if (await reads.GetMemberAsync(memberId, cancellationToken) is null)
                    throw new LegacyLogicalException(
                        $"For property '{property.Name}', data type is '{typeName}' but the provided user id is invalid.  Value provided was '{value.Value}'.");
                break;
        }
        return true;
    }

    /// <summary>The private Approve (spec 2.3.4): stamp, second pass, relations and renames, then MarkAsUpdated.</summary>
    private async Task ApproveAsync(Submittal submittal, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var value in submittal.Values)
        {
            value.Approved = Now();
            value.ApprovedBy = userId;
            value.IsDirty = true;
        }
        submittal.Reviewed = Now();
        submittal.ReviewedBy = userId;
        submittal.IsDirty = true;
        if (!await SaveAsync(submittal, isAutoApprove: false, userId, cancellationToken))
            return;

        var properties = await PropertiesAsync(submittal.Values.Select(v => v.PropertyId), cancellationToken);
        foreach (var value in submittal.Values)
        {
            var property = properties[value.PropertyId];
            (Guid From, Guid To, byte Type)? relation = (LegacyDataType)property.DataType switch
            {
                LegacyDataType.Relation_ParentChild => (value.AssetId, new Guid(value.Value!), RelationTypes.ParentChild),
                LegacyDataType.Relation_ChildParent => (new Guid(value.Value!), value.AssetId, RelationTypes.ParentChild),
                LegacyDataType.Dependency => (value.AssetId, new Guid(value.Value!), RelationTypes.Dependency),
                _ => null
            };
            // A user value with a role would be added to the role by a method that is a stub in the deployed release: no write.
            if (property.IsSystem && property.SystemType == (int)SystemType.AssetName)
                await writes.RenameAsync(value.AssetId, value.Value, cancellationToken);
            if (relation is { } r)
            {
                var created = Now();
                // RemoveExistingParentRelations is given the new parent: every child of that parent loses it (bug kept).
                if ((LegacyDataType)property.DataType == LegacyDataType.Relation_ChildParent)
                    await writes.RemoveParentRelationsAsync(r.From, r.Type, cancellationToken);
                if (!await writes.RelationExistsAsync(r.From, r.To, r.Type, cancellationToken))
                    await writes.SaveRelationAsync(Guid.NewGuid(), r.From, r.To, r.Type, created, created, submittal.CreatedBy, submittal.CreatedBy, cancellationToken);
            }
        }
        await assets.MarkAsUpdatedAsync(submittal.AssetId, submittal.CreatedBy, cancellationToken);
    }
}

/// <summary>
/// GenerateNarrative (spec M6 2.6): the submittal's sentence by asset kind, written to the submittal row. Background
/// work in the original and here.
/// </summary>
public sealed class NarrativeWriter(IAssetWriteProcedures writes, IAssetProcedures reads, MetadataLoader loader)
{
    /// <summary>MemberLayer.DisplayValue: "" for no member, the user id without names, else "Last, First [userId]".</summary>
    private async Task<string> MemberDisplayValueAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await reads.GetMemberAsync(memberId, cancellationToken);
        if (member is null)
            return string.Empty;
        if (string.IsNullOrEmpty(member.FirstName) && string.IsNullOrEmpty(member.LastName))
            return member.UserId;
        return $"{member.LastName}, {member.FirstName} [{member.UserId}]";
    }

    public async Task WriteAsync(Guid submittalId, CancellationToken cancellationToken)
    {
        var facts = await writes.GetNarrativeFactsAsync(submittalId, cancellationToken);
        if (facts is null)
            return;
        var hardware = (await loader.DescendantDictionaryAsync(NarrativeIds.HardwareType, cancellationToken)).Select(p => p.Key).ToHashSet();
        var narrative = hardware.Contains(facts.AssetTypeId) ? await HardwareAsync(facts, cancellationToken)
            : facts.AssetTypeId == NarrativeIds.SoftwareType ? $"The application '{facts.AssetName}' was {(WasCreation(facts) ? "created" : "updated")} by {facts.Submitter}."
            : facts.AssetTypeId == NarrativeIds.ReleaseType ? $"The application version '{facts.AssetName}' was {(WasCreation(facts) ? "created" : "updated")} by {facts.Submitter}."
            : $"The {facts.AssetType.ToLowerInvariant()} '{facts.AssetName}' was {(WasCreation(facts) ? "created" : "updated")} by {facts.Submitter}.";
        if (!string.IsNullOrEmpty(narrative))
            await writes.SetNarrativeAsync(submittalId, narrative, cancellationToken);
    }

    /// <summary>NarrativeHelper.WasCreation: the asset and the submittal share a calendar day.</summary>
    public static bool WasCreation(NarrativeFacts facts) => facts.AssetCreated.Date == facts.Submitted.Date;

    private async Task<string> HardwareAsync(NarrativeFacts facts, CancellationToken cancellationToken)
    {
        var type = facts.AssetType.ToLowerInvariant();
        if (!facts.IsInstance)
            return $"The {type} product code '{facts.AssetName}' was {(WasCreation(facts) ? "added" : "modified")} by {facts.Submitter}.";
        var values = facts.Values.ToDictionary(v => v.PropertyId, v => v.Value);
        if (!values.TryGetValue(NarrativeIds.StatusProperty, out var status))
            return $"The {type} asset code '{facts.AssetName}' ({facts.InstanceOf}) was {(WasCreation(facts) ? "added" : "updated")} by {facts.Submitter}.";
        if (!Guid.TryParse(status, out var statusId))
            return string.Empty;
        if (statusId == NarrativeIds.StatusAssigned)
        {
            if (values.TryGetValue(NarrativeIds.AssigneeProperty, out var assignee) && Guid.TryParse(assignee, out var memberId))
                return $"{facts.AssetName} ({facts.InstanceOf}) was assigned to {await MemberDisplayValueAsync(memberId, cancellationToken)} by {facts.Submitter}.";
            return string.Empty;
        }
        if (statusId == NarrativeIds.StatusInStock)
            return $"{facts.AssetName} ({facts.InstanceOf}) was returned to stock by {facts.Submitter}.";
        if (statusId == NarrativeIds.StatusDisposed)
            return $"{facts.AssetName} ({facts.InstanceOf}) was disposed of by {facts.Submitter}.";
        if (statusId == NarrativeIds.StatusLost)
            return $"{facts.AssetName} ({facts.InstanceOf}) was marked as 'Lost' by {facts.Submitter}.";
        return string.Empty;
    }
}
