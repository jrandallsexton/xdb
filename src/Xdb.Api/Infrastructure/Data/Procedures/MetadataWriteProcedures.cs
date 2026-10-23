using System;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class MetadataWriteProcedures(XdbDataContext db) : LegacyGatewayBase(db), IMetadataWriteProcedures
{
    private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;

    private async Task ProcedureAsync(string name, DynamicParameters parameters, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure(name, parameters, cancellationToken));

    private async Task InlineAsync(string sql, DynamicParameters parameters, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(sql, parameters, cancellationToken));

    private async Task<bool> CountIsOneAsync(string sql, Guid id, CancellationToken cancellationToken) =>
        await CountIsOneAsync(Inline(sql, new { Id = id }, cancellationToken));

    // ---- Asset types ----

    public Task SaveAssetTypeAsync(AssetTypeContract t, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_AssetType_Save", Supplied(
            ("Id", t.Id), ("Name", t.Name), ("Pluralization", NullIfEmpty(t.Pluralization)), ("DefaultViewId", t.DefaultViewId),
            ("DefaultInstanceViewId", t.DefaultInstanceViewId), ("DefinitionLabel", NullIfEmpty(t.DefinitionLabel)), ("InstanceLabel", NullIfEmpty(t.InstanceLabel)),
            ("ImageId", t.ImageId), ("AllowAssets", t.AllowAssets), ("Created", t.Created), ("CreatedBy", t.CreatedBy), ("Deleted", t.Deleted),
            ("DeletedBy", t.DeletedBy)), cancellationToken);

    public Task DeleteAssetTypeRelationAsync(Guid parentId, Guid childId, DateTime deleted, Guid deletedBy, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "UPDATE [AssetTypeRelations]",
            "SET [Deleted] = @Deleted,",
            "[DeletedBy] = @DeletedBy",
            "WHERE [ChildAssetTypeId] = @ChildId",
            "AND [ParentAssetTypeId] = @ParentId",
            "AND [Deleted] IS NULL",
            "AND [DeletedBy] IS NULL"),
            Supplied(("Deleted", deleted), ("DeletedBy", deletedBy), ("ChildId", childId), ("ParentId", parentId)), cancellationToken);

    public Task InsertAssetTypeRelationAsync(Guid id, Guid parentId, Guid childId, DateTime created, Guid createdBy, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "INSERT INTO [AssetTypeRelations] (",
            "Id, ParentAssetTypeId, ChildAssetTypeId, Created, CreatedBy)",
            "VALUES (",
            "@Id, @ParentId, @ChildId, @Created, @CreatedBy)"),
            Supplied(("Id", id), ("ParentId", parentId), ("ChildId", childId), ("Created", created), ("CreatedBy", createdBy)), cancellationToken);

    public Task DeleteInheritedMemberAsync(DateTime? deleted, Guid? deletedBy, Guid assetTypeId, Guid propertyId, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "UPDATE [AssetTypesProperties]",
            "SET [Deleted] = @Deleted, [DeletedBy] = @DeletedBy",
            "WHERE [AssetTypeId] = @AssetTypeId",
            "AND [PropertyId] = @PropertyId",
            "AND [IsInherited] = 1"),
            Supplied(("Deleted", deleted), ("DeletedBy", deletedBy), ("AssetTypeId", assetTypeId), ("PropertyId", propertyId)), cancellationToken);

    public Task SaveAssetTypePropertyRelationAsync(AssetTypePropertyRelationContract r, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_AssetTypePropertyRelation_Save", Supplied(
            ("Id", r.Id), ("AssetTypeId", r.AssetTypeId), ("PropertyId", r.PropertyId), ("IsInstance", r.IsInstance), ("IsInherited", r.IsInherited),
            ("IsInheritedValue", r.IsInheritedValue), ("Created", r.Created), ("CreatedBy", r.CreatedBy), ("Deleted", r.Deleted), ("DeletedBy", r.DeletedBy)),
            cancellationToken);

    // ---- Properties and picklists ----

    public Task SavePropertyAsync(PropertyContract p, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_Property_Save", Supplied(
            ("Id", p.Id), ("DataTypeId", p.DataType), ("Precision", p.Precision), ("PickListId", p.PickListId), ("AllowMultiValue", p.AllowMultiValue),
            ("IsOrdered", p.IsOrdered), ("Name", p.Name), ("DisplayValue", NullIfEmpty(p.DisplayValue)), ("Description", NullIfEmpty(p.Description)),
            ("AssetTypeId", p.AssetTypeId), ("AssetTypeIsInstance", p.AssetTypeIsInstance), ("Created", p.Created), ("Approved", p.Approved),
            ("LastModified", p.LastModified), ("CreatedBy", p.CreatedBy), ("ApprovedBy", p.ApprovedBy), ("LastModifiedBy", p.LastModifiedBy),
            ("Deleted", p.Deleted), ("DeletedBy", p.DeletedBy), ("RoleId", p.RoleId), ("IsSystem", p.IsSystem), ("SystemTypeId", p.SystemType)),
            cancellationToken);

    public Task<bool> IsLivePropertyAsync(Guid propertyId, CancellationToken cancellationToken) =>
        CountIsOneAsync("SELECT COUNT(*) FROM [Properties] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL", propertyId, cancellationToken);

    public Task SavePickListAsync(PickListContract l, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PickList_Save", Supplied(
            ("Id", l.Id), ("Name", l.Name), ("DisplayValue", NullIfEmpty(l.DisplayValue)), ("Description", l.Description),
            ("ServiceUrl", NullIfEmpty(l.ServiceUrl)), ("ServiceMethod", NullIfEmpty(l.ServiceMethod)), ("ServiceUsername", NullIfEmpty(l.ServiceUsername)),
            ("ServicePassword", NullIfEmpty(l.ServicePassword)), ("Created", l.Created), ("Approved", l.Approved), ("LastModified", l.LastModified),
            ("CreatedBy", l.CreatedBy), ("ApprovedBy", l.ApprovedBy), ("LastModifiedBy", l.LastModifiedBy), ("Deleted", l.Deleted), ("DeletedBy", l.DeletedBy),
            ("IsMemberList", l.IsMemberList)), cancellationToken);

    public Task<bool> IsLivePickListAsync(Guid pickListId, CancellationToken cancellationToken) =>
        CountIsOneAsync("SELECT COUNT(*) FROM [PickLists] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL", pickListId, cancellationToken);

    public Task SavePickListValueAsync(PickListValueContract v, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PickListValue_Save", Supplied(
            ("Id", v.Id), ("PickListId", v.PickListId), ("Value", v.Value), ("DisplayValue", NullIfEmpty(v.DisplayValue)), ("BGColor", NullIfEmpty(v.BGColor)),
            ("Order", v.Order), ("Created", v.Created), ("Approved", v.Approved), ("LastModified", v.LastModified), ("CreatedBy", v.CreatedBy),
            ("ApprovedBy", v.ApprovedBy), ("LastModifiedBy", v.LastModifiedBy), ("Deleted", v.Deleted), ("DeletedBy", v.DeletedBy)), cancellationToken);

    public Task SavePickListDependencyAsync(PickListDependencyContract d, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PickListDependency_Save", Supplied(
            ("Id", d.Id), ("ParentPickListId", d.ParentPickListId), ("ParentPickListValueId", d.ParentPickListValueId), ("ChildPickListId", d.ChildPickListId),
            ("ChildPickListValueId", d.ChildPickListValueId), ("Created", d.Created), ("CreatedBy", d.CreatedBy), ("Deleted", d.Deleted),
            ("DeletedBy", d.DeletedBy)), cancellationToken);

    // ---- Property groups ----

    public Task SavePropertyGroupAsync(PropertyGroupContract g, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PropertyGroup_Save", Supplied(
            ("Id", g.Id), ("Name", g.Name), ("DisplayValue", g.DisplayValue), ("AssetTypeId", g.AssetTypeId), ("IsInstance", g.IsInstance), ("Created", g.Created),
            ("LastModified", g.LastModified), ("Deleted", g.Deleted), ("CreatedBy", g.CreatedBy), ("LastModifiedBy", g.LastModifiedBy), ("DeletedBy", g.DeletedBy)),
            cancellationToken);

    public Task SavePropertyGroupRelationAsync(PropertyGroupPropertyRelationContract r, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PropertyGroupPropertyRelation_Save", Supplied(
            ("Id", r.Id), ("PropertyGroupId", r.PropertyGroupId), ("PropertyId", r.PropertyId), ("Order", r.Order), ("IsRequired", r.IsRequired),
            ("IsReadOnly", r.IsReadOnly), ("DefaultValue", NullIfEmpty(r.DefaultValue)), ("Created", r.Created), ("Deleted", r.Deleted), ("CreatedBy", r.CreatedBy),
            ("DeletedBy", r.DeletedBy)), cancellationToken);

    public Task<bool> IsLivePropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken) =>
        CountIsOneAsync("SELECT COUNT(*) FROM [PropertyGroups] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL", propertyGroupId, cancellationToken);

    public Task KillPropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PropertyGroup_Kill", Supplied(("Id", propertyGroupId)), cancellationToken);

    // ---- Views ----

    public Task SaveViewAsync(ViewContract v, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_View_Save", Supplied(
            ("Id", v.Id), ("Name", v.Name), ("DisplayValue", v.DisplayValue), ("Description", v.Description), ("IsInstance", v.IsInstance),
            ("AssetTypeId", v.AssetTypeId), ("DriverCaption", NullIfEmpty(v.DriverCaption)), ("NewItemCaption", NullIfEmpty(v.NewItemCaption)),
            ("ConfirmationLabel", NullIfEmpty(v.ConfirmationLabel)), ("IsReadOnly", v.IsReadOnly), ("IsStandAlone", v.IsStandAlone), ("IsCreation", v.IsCreation),
            ("FilterLogic", NullIfEmpty(v.FilterLogic)), ("Created", v.Created), ("LastModified", v.LastModified), ("CreatedBy", v.CreatedBy),
            ("LastModifiedBy", v.LastModifiedBy), ("Deleted", v.Deleted), ("DeletedBy", v.DeletedBy)), cancellationToken);

    public Task SaveViewGroupRelationAsync(ViewPropertyGroupRelationContract r, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_ViewPropertyGroupRelation_Save", Supplied(
            ("Id", r.Id), ("ViewId", r.ViewId), ("PropertyGroupId", r.PropertyGroupId), ("Order", r.Order), ("Created", r.Created), ("CreatedBy", r.CreatedBy),
            ("Deleted", r.Deleted), ("DeletedBy", r.DeletedBy)), cancellationToken);

    public Task DeleteViewFiltersAsync(Guid viewId, CancellationToken cancellationToken) =>
        InlineAsync("DELETE FROM [ViewFilters] WHERE [ViewId] = @Id", Supplied(("Id", viewId)), cancellationToken);

    public Task InsertViewFilterAsync(Guid id, Guid viewId, FilterContract filter, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "INSERT INTO [ViewFilters] ([Id], [ViewId], [PropertyId], [OperatorId], [Value], [Order])",
            "VALUES (@Id, @ViewId, @PropertyId, @OperatorId, @Value, @Order)"),
            Supplied(("Id", id), ("ViewId", viewId), ("PropertyId", filter.PropertyId), ("OperatorId", filter.OperatorId), ("Value", filter.Value),
                ("Order", filter.Order)), cancellationToken);

    public Task KillViewAsync(Guid viewId, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_View_Kill", Supplied(("Id", viewId)), cancellationToken);

    // ---- Quick filters ----

    public Task SaveMemberViewAsync(Guid id, Guid memberId, Guid viewId, string? name, string? filterExpression, string? filterString,
        CancellationToken cancellationToken) =>
        ProcedureAsync("spr_MemberView_Save", Supplied(
            ("Id", id), ("MemberId", memberId), ("ViewId", viewId), ("Name", name), ("FilterExp", filterExpression), ("FilterString", filterString)), cancellationToken);

    public Task DeleteMemberViewFiltersAsync(Guid memberViewId, CancellationToken cancellationToken) =>
        InlineAsync("DELETE FROM [MemberViewFilters] WHERE [MemberViewId] = @Id", Supplied(("Id", memberViewId)), cancellationToken);

    public Task InsertMemberViewFilterAsync(Guid id, Guid memberViewId, FilterContract filter, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "INSERT INTO [MemberViewFilters] ([Id], [MemberViewId], [PropertyId], [OperatorId], [Value], [Order])",
            "VALUES (@Id, @MemberViewId, @PropertyId, @OperatorId, @Value, @Order)"),
            Supplied(("Id", id), ("MemberViewId", memberViewId), ("PropertyId", filter.PropertyId), ("OperatorId", filter.OperatorId), ("Value", filter.Value),
                ("Order", filter.Order)), cancellationToken);

    public Task DeleteMemberFilterAsync(Guid memberViewId, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "DELETE FROM [MemberViewFilters] WHERE [MemberViewId] = @Id",
            "DELETE FROM [MemberViews] WHERE [Id] = @Id"), Supplied(("Id", memberViewId)), cancellationToken);

    public Task DeleteViewClientAsync(Guid memberId, Guid viewId, CancellationToken cancellationToken) =>
        InlineAsync("DELETE FROM [ViewClients] WHERE [ViewId] = @ViewId AND [UserId] = @UserId", Supplied(("ViewId", viewId), ("UserId", memberId)), cancellationToken);
}
