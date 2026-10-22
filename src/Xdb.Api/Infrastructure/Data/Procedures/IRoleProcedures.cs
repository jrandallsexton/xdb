using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Roles.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>The original's role reads and writes (RoleDal, RoleViewRelationDal, RoleProcessRelationDal, ErrorDal).</summary>
public interface IRoleProcedures
{
    /// <summary>
    /// RoleDal.Get over spr_Role_Get's four result sets, with the original's per-column NULL behaviour (a NULL
    /// Created/CreatedBy or name fails; an operator id outside 0..6 fails); null when absent.
    /// </summary>
    Task<RoleContract?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken);
    /// <summary>RoleDal.Roles_GetDictionary: names (" (deleted)" appended), live and/or active only per the flags, by name.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetRolesDictionaryAsync(bool includeDeleted, bool includeInactive, CancellationToken cancellationToken);
    /// <summary>spr_Members_GetDictionary_InRole / _NotInRole through the procedure GetDictionary (a repeated key fails).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMembersAsync(Guid roleId, bool inRole, CancellationToken cancellationToken);
    /// <summary>Live reports in (or not in) the role's live RoleReports rows, by name.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetReportsAsync(Guid roleId, bool inRole, CancellationToken cancellationToken);
    /// <summary>spr_Role_Get_MemberIds (guarded, NULLs skipped, no order).</summary>
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid roleId, CancellationToken cancellationToken);
    /// <summary>MemberLayer.GetUserId: the member's user id, trimmed; "" when absent.</summary>
    Task<string> GetUserIdAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>spr_Role_Save (insert or full update, then the members' LastModified touch); nulls not supplied.</summary>
    Task SaveRoleAsync(RoleContract role, CancellationToken cancellationToken);
    /// <summary>One inline batch, one command (a compile error rejects all of it; a runtime error ends one statement).</summary>
    Task ExecuteBatchAsync(string batch, CancellationToken cancellationToken);
    Task SaveViewRelationAsync(RoleViewRelationContract relation, CancellationToken cancellationToken);
    Task SaveProcessRelationAsync(RoleProcessRelationContract relation, CancellationToken cancellationToken);
    /// <summary>spr_Role_ContainsMember compared with 1.</summary>
    Task<bool> ContainsMemberAsync(Guid memberId, Guid roleId, CancellationToken cancellationToken);
    Task KillRoleAsync(Guid roleId, CancellationToken cancellationToken);
    /// <summary>ErrorDal.Error_Save: one Errors row (Created defaults to now).</summary>
    Task SaveErrorAsync(string errorId, string message, Guid memberId, CancellationToken cancellationToken);
}
