using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.GeneratedObjects.Views;

public interface IRoleViewGenerator
{
    /// <summary>(Re)creates a role's row-security views from the role as stored.</summary>
    /// <returns>The number of views created.</returns>
    Task<int> GenerateAsync(Guid roleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The view part of the original's RoleLayer.Save: one view per non-inherited asset type and permission that has
/// filters and none of them is CURRENT_USER (those are evaluated at run time). The view name omits definition vs
/// instance, so processing in spr_Role_Get's order lets the last one win, as in the original.
/// </summary>
public sealed class RoleViewGenerator(IGeneratedObjectProcedures procedures, ViewInputLoader loader) : IRoleViewGenerator
{
    private const int DdlTimeout = 3000;

    private const string CurrentUserFilterValue = "CURRENT_USER";

    private static readonly ViewColumn NameColumn = new(SystemPropertyIds.AssetName, null, null, "Asset Name", null);

    public async Task<int> GenerateAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var views = 0;
        foreach (var assetType in await procedures.GetRoleAssetTypesAsync(roleId, cancellationToken))
        {
            if (assetType.IsInherited)
                continue;

            foreach (var permission in assetType.Permissions)
            {
                if (permission.Filters.Count == 0 || permission.Filters.Any(f => f.Value == CurrentUserFilterValue))
                    continue;

                var filters = permission.Filters.Select(f => new ViewFilter(f.PropertyId, f.OperatorId, f.Operator, f.Value)).ToList();
                var input = await loader.LoadAsync(assetType.AssetTypeId, assetType.IsInstance, [NameColumn], filters,
                    permission.FilterExpression, cancellationToken);
                var select = ViewSql.BuildSelect(input);

                var viewName = GeneratedObjectNames.RoleView(roleId, assetType.AssetTypeId, permission.PermissionId);
                await procedures.ExecuteAsync(ViewSql.DropView(viewName), DdlTimeout, cancellationToken);
                await procedures.ExecuteAsync(ViewSql.CreateView(viewName, select), DdlTimeout, cancellationToken);
                views++;
            }
        }

        return views;
    }
}
