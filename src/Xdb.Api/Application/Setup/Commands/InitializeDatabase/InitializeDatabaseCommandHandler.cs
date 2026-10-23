using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Setup.Commands.InitializeDatabase;

public sealed record InitializeDatabaseCommand;

/// <param name="FirstAdminCreated">The configured first administrator was created by this run.</param>
/// <param name="Regenerated">Generated objects were built (the database had none); null when they already existed.</param>
public sealed record InitializeDatabaseResult(bool FirstAdminCreated, RegenerateAllResult? Regenerated);

/// <summary>The first real administrator of a fresh install (section "Seed:FirstAdmin"). Unset: none is created.</summary>
public sealed class FirstAdminOptions
{
    public const string SectionName = "Seed:FirstAdmin";

    /// <summary>The account name the caller is identified by (Members.UserId).</summary>
    public string? UserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}

public interface IInitializeDatabaseCommandHandler
{
    Task<Result<InitializeDatabaseResult>> ExecuteAsync(InitializeDatabaseCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Startup work after migrations that a fresh install needs and an existing database does not:
/// the configured first administrator (the original never seeded one; admin rights are membership in the Admin
/// role), then, if no generated objects exist yet, a full regeneration. An existing database keeps its generated
/// objects as they are.
/// </summary>
public sealed class InitializeDatabaseCommandHandler(
    XdbDataContext db,
    IGeneratedObjectProcedures procedures,
    IRegenerateAllCommandHandler regenerateAll,
    IOptions<FirstAdminOptions> firstAdmin,
    ILogger<InitializeDatabaseCommandHandler> logger) : IInitializeDatabaseCommandHandler
{
    public async Task<Result<InitializeDatabaseResult>> ExecuteAsync(InitializeDatabaseCommand command, CancellationToken cancellationToken = default)
    {
        var adminCreated = await EnsureFirstAdminAsync(cancellationToken);

        RegenerateAllResult? regenerated = null;
        if (!await procedures.AnyGenTablesAsync(cancellationToken))
        {
            logger.LogInformation("No generated objects found: building them.");
            var result = await regenerateAll.ExecuteAsync(new RegenerateAllCommand(), cancellationToken);
            regenerated = result.Value;
        }

        return new Success<InitializeDatabaseResult>(new InitializeDatabaseResult(adminCreated, regenerated));
    }

    private async Task<bool> EnsureFirstAdminAsync(CancellationToken cancellationToken)
    {
        var options = firstAdmin.Value;
        if (string.IsNullOrWhiteSpace(options.UserId))
            return false;

        var userId = options.UserId.Trim();
        var memberId = await db.Members.AsNoTracking()
            .Where(m => m.UserId == userId && m.Deleted == null)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var created = false;
        if (memberId is null)
        {
            memberId = Guid.NewGuid();
            db.Members.Add(new Member
            {
                Id = memberId.Value,
                UserId = userId,
                Fname = options.FirstName,
                Lname = options.LastName,
                CreatedBy = SystemMemberIds.Admin
            });
            // No User asset: a fresh install holds no assets (the original allowed members without one; such a
            // member is only missing from the members pick list).
            created = true;
        }

        var isAdmin = await db.RoleMembers.AsNoTracking()
            .AnyAsync(rm => rm.RoleId == SystemRoleIds.Admin && rm.MemberId == memberId && rm.Deleted == null, cancellationToken);
        if (!isAdmin)
        {
            db.RoleMembers.Add(new RoleMember
            {
                Id = Guid.NewGuid(),
                RoleId = SystemRoleIds.Admin,
                MemberId = memberId.Value,
                CreatedBy = SystemMemberIds.Admin
            });
        }

        if (created || !isAdmin)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("First administrator {UserId} ensured (member created: {Created}).", userId, created);
        }

        return created;
    }
}
