using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Admin.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Admin;

/// <summary>
/// Behaviour of the original that needed a Windows-only component or an external system (spec M9 section 4).
/// Deferred by owner decision 2026-10-23: the port fails where the original failed for every input it answers here.
/// </summary>
public static class DeferredAdmin
{
    /// <summary>D1: the backend Windows service's status (service control manager).</summary>
    public static Exception ServiceStatus() => new NotSupportedException("Deferred: Windows service control is a Windows-only dependency (D1).");

    /// <summary>D2: hardware queries through WMI on the local or a remote machine.</summary>
    public static Exception WmiQuery() => new NotSupportedException("Deferred: WMI queries are a Windows-only dependency (D2).");

    /// <summary>D3: mail through the original's internal SMTP relay.</summary>
    public static Exception SendMail() => new NotSupportedException("Deferred: mail needs an SMTP relay (D3).");
}

public interface IAdminQueryHandler
{
    IReadOnlyList<KeyValuePair<int, string>> ApplicationInstances();
    List<string> WmiQueryableObjects();
    Task<Result<HelpSectionContract?>> CreateHelpSectionAsync(Guid userId, CancellationToken cancellationToken = default);
    Result<ServiceModuleMetadataContract> CreateServiceModule();
    Task<Result<ServiceModuleMetadataContract>> GetServiceModuleAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetServiceModulesAsync(CancellationToken cancellationToken = default);
}

/// <summary>The administration reads (spec M9 section 1).</summary>
public sealed class AdminQueryHandler(IAdminProcedures admin, IRoleProcedures roles, IDateTimeProvider clock) : IAdminQueryHandler
{
    /// <summary>EApplicationInstance in declaration order (generic environment tiers).</summary>
    private static readonly KeyValuePair<int, string>[] Instances = [new(0, "LOCAL"), new(1, "DEV"), new(2, "QA"), new(3, "STG"), new(4, "PROD")];

    /// <summary>EQueryableObjects in declaration order: WMI class names (Win32_ prefix dropped).</summary>
    private static readonly string[] QueryableObjects =
    [
        "BaseBoard", "Battery", "BIOS", "Bus", "CDROMDrive", "DiskDrive", "DMAChannel", "Fan", "FloppyController", "FloppyDrive", "IDEController", "IRQResource",
        "Keyboard", "MemoryDevice", "NetworkAdapter", "NetworkAdapterConfiguration", "OnBoardDevice", "ParallelPort", "PCMCIController", "PhysicalMedia",
        "PhysicalMemory", "PortConnector", "PortResource", "POTSModem", "Processor", "SCSIController", "SerialPort", "SerialPortConfiguration", "Software",
        "SoundDevice", "SystemEnclosure", "TapeDrive", "TemperatureProbe", "UninterruptiblePowerSupply", "USBController", "USBHub", "VideoController", "VoltageProbe"
    ];

    private const int NameLimit = 50;

    private DateTime Now() => clock.UtcNow().ToLocalTime();

    public IReadOnlyList<KeyValuePair<int, string>> ApplicationInstances() => Instances;

    public List<string> WmiQueryableObjects() => [.. QueryableObjects];

    /// <summary>HelpSection_Create: a new section for an administrator of the queried member (exactly one live Admin row), else null.</summary>
    public async Task<Result<HelpSectionContract?>> CreateHelpSectionAsync(Guid userId, CancellationToken cancellationToken = default) =>
        new Success<HelpSectionContract?>(await roles.ContainsMemberAsync(userId, SystemRoleIds.Admin, cancellationToken)
            ? new HelpSectionContract { Id = Guid.NewGuid(), Title = null!, Content = null! }
            : null);

    /// <summary>ServiceModule_Create: the constructor's object with a new id, created now, new and dirty.</summary>
    public Result<ServiceModuleMetadataContract> CreateServiceModule()
    {
        var module = ServiceModuleMetadataContract.Fresh(Now());
        module.IsDirty = true;
        return new Success<ServiceModuleMetadataContract>(module);
    }

    /// <summary>
    /// ServiceModuleDal.ServiceModule_Get: never null. Without a row, the constructor's object (random id, not dirty);
    /// with one, the selected columns (a NULL string column fails) and the rest at their initial values.
    /// </summary>
    public async Task<Result<ServiceModuleMetadataContract>> GetServiceModuleAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        var module = ServiceModuleMetadataContract.Fresh(Now());
        if (await admin.GetServiceModuleAsync(moduleId, cancellationToken) is not { } row)
            return new Success<ServiceModuleMetadataContract>(module);
        static string Required(string? text) => text ?? throw new SqlNullValueException("Data is Null. This method or property cannot be called on Null values.");
        var name = Required(row.Name);
        module.Id = moduleId;
        module.Name = name.Length > NameLimit ? name[..(NameLimit - 1)] : name;
        module.Description = Required(row.Description);
        module.AssemblyName = Required(row.AssemblyName);
        module.ClassName = Required(row.ClassName);
        module.AssemblyVersion = Required(row.AssemblyVersion);
        module.Downloaded = row.Downloaded;
        module.Created = row.Created;
        module.LastRan = row.LastRan;
        module.LastCompleted = row.LastCompleted;
        module.IsDirty = true;
        return new Success<ServiceModuleMetadataContract>(module);
    }

    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetServiceModulesAsync(CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await admin.GetServiceModulesDictionaryAsync(cancellationToken));
}

public interface IAdminCommandHandler
{
    Task<Result<bool>> SaveHelpSectionAsync(HelpSectionContract? section, CancellationToken cancellationToken = default);
    Task<Result<bool>> SaveServiceModuleAsync(ServiceModuleMetadataContract? module, CancellationToken cancellationToken = default);
}

/// <summary>The administration writes (spec M9 section 2): no validation, no identity, no transaction.</summary>
public sealed class AdminCommandHandler(IAdminProcedures admin) : IAdminCommandHandler
{
    /// <summary>HelpSectionDal.HelpSection_Save: insert when the id has no row, else update.</summary>
    public async Task<Result<bool>> SaveHelpSectionAsync(HelpSectionContract? section, CancellationToken cancellationToken = default)
    {
        var s = section ?? throw new NullReferenceException("Object reference not set to an instance of an object.");
        if (await admin.CountHelpSectionAsync(s.Id, cancellationToken) == 0)
            await admin.InsertHelpSectionAsync(s.Id, s.Title, s.ImageId, s.Order, s.Content, cancellationToken);
        else
            await admin.UpdateHelpSectionAsync(s.Id, s.Title, s.ImageId, s.Order, s.Content, cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> SaveServiceModuleAsync(ServiceModuleMetadataContract? module, CancellationToken cancellationToken = default)
    {
        await admin.SaveServiceModuleAsync(module ?? throw new NullReferenceException("Object reference not set to an instance of an object."), cancellationToken);
        return new Success<bool>(true);
    }
}
