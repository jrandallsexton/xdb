using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Admin.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A ServiceModules row as ServiceModuleDal.ServiceModule_Get read it (string columns as stored, NULL included).</summary>
public sealed record ServiceModuleRow(string? Name, string? Description, string? AssemblyName, string? ClassName, string? AssemblyVersion, DateTime? Downloaded,
    DateTime Created, DateTime? LastRan, DateTime? LastCompleted);

/// <summary>The original's help-section and service-module data access (spec M9), with its SQL text.</summary>
public interface IAdminProcedures
{
    /// <summary>The COUNT of HelpContent rows with the id (ExecuteScalarInLine: -1 when not a number).</summary>
    Task<int> CountHelpSectionAsync(Guid id, CancellationToken cancellationToken);
    Task InsertHelpSectionAsync(Guid id, string? title, Guid imageId, int order, string? content, CancellationToken cancellationToken);
    Task UpdateHelpSectionAsync(Guid id, string? title, Guid imageId, int order, string? content, CancellationToken cancellationToken);

    Task<ServiceModuleRow?> GetServiceModuleAsync(Guid moduleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetServiceModulesDictionaryAsync(CancellationToken cancellationToken);
    /// <summary>spr_ServiceModule_Save with the original's twelve parameters (never @Downloaded, so every save clears it).</summary>
    Task SaveServiceModuleAsync(ServiceModuleMetadataContract module, CancellationToken cancellationToken);
}
