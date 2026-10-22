using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.Common.Identity;

/// <summary>
/// Helpers.GetCurrentUserId: the member whose user id is the caller's account name, or the empty Guid. The port has no
/// authentication yet, so the account name is configured (the original resolved its Windows identity; anonymous
/// requests resolved to the application pool's account).
/// </summary>
public sealed class CurrentMember(IMetadataProcedures metadata, IOptions<LegacyEnvironmentOptions> environment)
{
    public Task<Guid> GetIdAsync(CancellationToken cancellationToken) =>
        metadata.GetMemberIdByUserIdAsync(environment.Value.CurrentUserName, cancellationToken);
}
