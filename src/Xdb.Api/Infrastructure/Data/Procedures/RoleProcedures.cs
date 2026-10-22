using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Xdb.Api.Application.Roles.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class RoleProcedures(XdbDataContext db) : LegacyGatewayBase(db), IRoleProcedures
{
    private const int MaxOperatorId = 6;

    public async Task<RoleContract?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Role_Get", new { Id = roleId }, cancellationToken));
        if (!reader.HasRows)
            return null;
        await reader.ReadAsync(cancellationToken);
        var role = new RoleContract
        {
            Id = roleId,
            Name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
            Description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            IsSystem = !reader.IsDBNull(2) && (bool)reader.GetValue(2),
            IsActive = !reader.IsDBNull(3) && (bool)reader.GetValue(3),
            Created = reader.GetDateTime(4),
            CreatedBy = reader.GetGuid(5),
            LastModified = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            LastModifiedBy = reader.IsDBNull(7) ? null : reader.GetGuid(7),
            Deleted = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            DeletedBy = reader.IsDBNull(9) ? null : reader.GetGuid(9),
            IsNew = false,
            IsDirty = false
        };

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            role.Members!.Add(new RoleMemberContract { Id = reader.GetGuid(0), MemberId = reader.GetGuid(1), MemberName = reader.GetString(2) });
            _ = reader.GetDateTime(3);
            _ = reader.GetGuid(4);
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            role.Reports!.Add(new RoleReportContract
            {
                Id = reader.GetGuid(0), ReportId = reader.GetGuid(1), ReportName = reader.GetString(2), Created = reader.GetDateTime(3), CreatedBy = reader.GetGuid(4)
            });

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var assetTypeRowId = reader.GetGuid(0);
            var assetType = role.AssetTypes!.Find(a => a.Id == assetTypeRowId);
            if (assetType is null)
            {
                assetType = new RoleAssetTypeContract
                {
                    Id = assetTypeRowId,
                    AssetTypeId = reader.GetGuid(1),
                    IsInstance = (bool)reader.GetValue(2),
                    IsInherited = (bool)reader.GetValue(3),
                    InheritFrom = reader.IsDBNull(4) ? null : reader.GetGuid(4)
                };
                role.AssetTypes.Add(assetType);
            }
            if (reader.IsDBNull(5))
                continue;
            var permissionRowId = reader.GetGuid(5);
            var permission = assetType.Permissions!.Find(p => p.Id == permissionRowId);
            if (permission is null)
            {
                permission = new RolePermissionContract
                {
                    Id = permissionRowId,
                    PermissionId = reader.GetInt32(6),
                    IsIncludesPermission = (bool)reader.GetValue(7),
                    FilterExpression = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                };
                assetType.Permissions.Add(permission);
            }
            if (reader.IsDBNull(9))
                continue;
            var filterId = reader.GetGuid(9);
            var order = reader.GetInt32(10);
            var propertyId = reader.GetGuid(11);
            var property = reader.GetString(12);
            var operatorId = reader.GetInt32(13);
            if (operatorId is < 0 or > MaxOperatorId)
                throw new InvalidOperationException("Invalid value for EFilterOperator");
            permission.Filters!.Add(new FilterContract
            {
                Id = filterId, Order = order, PropertyId = propertyId, Property = property, OperatorId = operatorId, Operator = reader.GetString(14),
                Value = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                DisplayValue = reader.IsDBNull(16) ? string.Empty : reader.GetString(16)
            });
        }
        return role;
    }

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetRolesDictionaryAsync(bool includeDeleted, bool includeInactive, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], [Name], [Deleted] FROM [Roles] WITH (NoLock)");
        if (!includeDeleted)
        {
            Line(sb, "WHERE [Deleted] IS NULL");
            if (!includeInactive)
                Line(sb, "AND [IsActive] = 1");
        }
        else if (!includeInactive)
        {
            Line(sb, "WHERE [IsActive] = 1");
        }
        Line(sb, "ORDER BY [Name]");
        var result = new List<KeyValuePair<Guid, string>>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), null, cancellationToken));
        if (!reader.HasRows)
            return result;
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new KeyValuePair<Guid, string>(reader.GetGuid(0), reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(1) + " (deleted)"));
        return result;
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMembersAsync(Guid roleId, bool inRole, CancellationToken cancellationToken) =>
        LegacyDictionaryAsync(Procedure(inRole ? "spr_Members_GetDictionary_InRole" : "spr_Members_GetDictionary_NotInRole", new { RoleId = roleId }, cancellationToken),
            firstWins: false, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetReportsAsync(Guid roleId, bool inRole, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT R.[Id], R.[ReportName]",
            "FROM [Reports] R",
            inRole ? "WHERE R.[Id] IN (" : "WHERE R.[Id] NOT IN (",
            "SELECT [ReportId] FROM [RoleReports]",
            "WHERE [RoleId] = @RoleId AND [Deleted] IS NULL AND [DeletedBy] IS NULL )",
            "AND R.[Deleted] IS NULL AND R.[DeletedBy] IS NULL",
            "ORDER BY R.[ReportName] "), new { RoleId = roleId }, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Role_Get_MemberIds", new { RoleId = roleId }, cancellationToken));
        if (!reader.HasRows)
            return ids;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
                ids.Add(reader.GetGuid(0));
        }
        return ids;
    }

    public Task<string> GetUserIdAsync(Guid memberId, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [UserId] FROM [Members] WITH (NoLock) WHERE [Id] = @Id", new { Id = memberId }, cancellationToken);

    public async Task SaveRoleAsync(RoleContract role, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", role.Id);
        parameters.Add("Name", role.Name);
        parameters.Add("Description", role.Description);
        parameters.Add("IsSystem", role.IsSystem);
        parameters.Add("IsActive", role.IsActive);
        parameters.Add("Created", role.Created, DbType.DateTime);
        parameters.Add("CreatedBy", role.CreatedBy);
        parameters.Add("LastModified", role.LastModified, DbType.DateTime);
        parameters.Add("LastModifiedBy", role.LastModifiedBy);
        parameters.Add("Deleted", role.Deleted, DbType.DateTime);
        parameters.Add("DeletedBy", role.DeletedBy);
        parameters.Add("RoleLead", role.RoleLead);
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Role_Save", parameters, cancellationToken));
    }

    public async Task ExecuteBatchAsync(string batch, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition(batch, null, commandType: CommandType.Text, commandTimeout: ProcedureTimeout,
            cancellationToken: cancellationToken));

    public async Task SaveViewRelationAsync(RoleViewRelationContract relation, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", relation.Id);
        parameters.Add("RoleId", relation.RoleId);
        parameters.Add("ViewId", relation.ViewId);
        parameters.Add("IsAutoApprove", relation.IsAutoApprove);
        parameters.Add("Created", relation.Created, DbType.DateTime);
        parameters.Add("CreatedBy", relation.CreatedBy);
        parameters.Add("Deleted", relation.Deleted, DbType.DateTime);
        parameters.Add("DeletedBy", relation.DeletedBy);
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_RoleViewRelation_Save", parameters, cancellationToken));
    }

    public async Task SaveProcessRelationAsync(RoleProcessRelationContract relation, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", relation.Id);
        parameters.Add("RoleId", relation.RoleId);
        parameters.Add("AssetTypeId", relation.AssetTypeId);
        parameters.Add("ProcessId", relation.ProcessId);
        parameters.Add("Created", relation.Created, DbType.DateTime);
        parameters.Add("CreatedBy", relation.CreatedBy);
        parameters.Add("Deleted", relation.Deleted, DbType.DateTime);
        parameters.Add("DeletedBy", relation.DeletedBy);
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_RoleProcessRelation_Save", parameters, cancellationToken));
    }

    public Task<bool> ContainsMemberAsync(Guid memberId, Guid roleId, CancellationToken cancellationToken) =>
        CountIsOneAsync(Procedure("spr_Role_ContainsMember", new { RoleId = roleId, MemberId = memberId }, cancellationToken));

    public async Task KillRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Role_Kill", new { RoleId = roleId }, cancellationToken));

    public async Task SaveErrorAsync(string errorId, string message, Guid memberId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline("INSERT INTO [Errors] (Id, Message, MemberId) VALUES (@Id, @Msg, @MemberId)",
            new { Id = errorId, Msg = message, MemberId = memberId }, cancellationToken));
}
