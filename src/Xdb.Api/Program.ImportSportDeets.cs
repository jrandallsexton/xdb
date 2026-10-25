using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Npgsql;

using Xdb.Api.Application.Assets.Commands.CreateAsset;
using Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.PickLists.Commands.CreatePickList;
using Xdb.Api.Application.Properties.Commands.CreateProperty;
using Xdb.Api.Application.PropertyGroups.Commands.CreatePropertyGroup;
using Xdb.Api.Application.Views.Commands.CreateView;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Core.Common;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api;

/// <summary>
/// The first sportDeets import (docs/13-import-venue.md), run with <c>dotnet run -- import-sportdeets [--reset]</c> under
/// the SportDeets environment. Not the long-term home: it reads the Producer database directly and writes through the
/// Create* slices. Every step looks for what it would create (by name) and reuses it, so a re-run, or a new type that
/// shares properties and pick lists, only adds what is missing. Rows whose asset already exists are skipped.
/// </summary>
public partial class Program
{
    /// <summary>First argument that runs the sportDeets import after migrations and setup, then exits.</summary>
    public const string ImportSportDeetsVerb = "import-sportdeets";

    /// <summary>With the import verb: drop the import database first, so migrations rebuild it as a fresh install.</summary>
    public const string ResetOption = "--reset";

    /// <summary>The only database the import (and its reset) will touch.</summary>
    private const string SportDeetsDatabase = "XdbSportDeets";

    /// <summary>A user-secrets store of its own, holding only the Producer connection string.</summary>
    private const string SportDeetsSecretsId = "xdb-sportdeets";

    private const string ProducerConnectionStringName = "SportDeetsProducer";

    /// <summary>System properties a group may name; they are never created or linked.</summary>
    private static readonly Dictionary<string, Guid> SystemProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AssetName"] = SystemPropertyIds.AssetName,
        ["Description"] = SystemPropertyIds.Description,
        ["InstanceOf"] = SystemPropertyIds.InstanceOf
    };

    /// <param name="PickList">The pick list's name, for PickList properties; shared by every property naming it.</param>
    /// <param name="FixedValues">The list's values; null takes the distinct values found in the rows.</param>
    private sealed record ImportProperty(string Name, DataType DataType, string? PickList = null, IReadOnlyList<string>? FixedValues = null);

    /// <param name="Properties">Import property names, or a key of <see cref="SystemProperties"/>.</param>
    private sealed record ImportGroup(string Name, IReadOnlyList<string> Properties);

    /// <param name="Values">Property name to text; null or blank writes no value. Pick list values are given as their text.</param>
    private sealed record ImportRow(Guid Id, string Name, IReadOnlyDictionary<string, string?> Values);

    /// <summary>One sportDeets table as one asset type: a single definition, each row an instance of it.</summary>
    private sealed record ImportType(
        string Name,
        string Pluralization,
        string DefinitionLabel,
        string InstanceLabel,
        string DefinitionName,
        IReadOnlyList<ImportProperty> Properties,
        IReadOnlyList<ImportGroup> Groups,
        string ViewName,
        string Query,
        Func<DbDataReader, ImportRow> Map);

    // ---- What to import ----

    private static readonly ImportType Venue = new(
        Name: "Venue",
        Pluralization: "Venues",
        DefinitionLabel: "Venue Kind",
        InstanceLabel: "Venue",
        DefinitionName: "Venue",
        Properties:
        [
            new("Short Name", DataType.String),
            new("Surface", DataType.PickList, PickList: "Surface", FixedValues: ["Grass", "Artificial turf"]),
            new("Setting", DataType.PickList, PickList: "Setting", FixedValues: ["Outdoor", "Indoor"]),
            new("Capacity", DataType.Int),
            new("City", DataType.String),
            new("State", DataType.PickList, PickList: "State"),
            new("Postal Code", DataType.String),
            new("Country", DataType.PickList, PickList: "Country"),
            new("Latitude", DataType.Float),
            new("Longitude", DataType.Float),
            new("Slug", DataType.String)
        ],
        Groups:
        [
            new("Facility", ["AssetName", "InstanceOf", "Short Name", "Surface", "Setting", "Capacity"]),
            new("Location", ["City", "State", "Postal Code", "Country", "Latitude", "Longitude"]),
            new("sportDeets", ["Slug"])
        ],
        ViewName: "Venues",
        // Producer, sdProducer.FootballNcaa (NCAAFB only, for now). Read-only.
        Query: """
            SELECT "Id", "Name", "ShortName", "IsGrass", "IsIndoor", "Slug", "Capacity",
                   "City", "State", "PostalCode", "Country", "Latitude", "Longitude"
            FROM public."Venue"
            ORDER BY "Name"
            """,
        Map: reader => new ImportRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetString(reader.GetOrdinal("Name")).Trim(),
            new Dictionary<string, string?>
            {
                ["Short Name"] = Text(reader, "ShortName"),
                ["Surface"] = reader.GetBoolean(reader.GetOrdinal("IsGrass")) ? "Grass" : "Artificial turf",
                ["Setting"] = reader.GetBoolean(reader.GetOrdinal("IsIndoor")) ? "Indoor" : "Outdoor",
                // Every NCAAFB row has 0 today: unknown, so no value.
                ["Capacity"] = reader.GetInt32(reader.GetOrdinal("Capacity")) is > 0 and var capacity ? capacity.ToString(CultureInfo.InvariantCulture) : null,
                ["City"] = Text(reader, "City"),
                ["State"] = Text(reader, "State"),
                ["Postal Code"] = Text(reader, "PostalCode"),
                ["Country"] = Text(reader, "Country"),
                ["Latitude"] = reader.GetDecimal(reader.GetOrdinal("Latitude")).ToString(CultureInfo.InvariantCulture),
                ["Longitude"] = reader.GetDecimal(reader.GetOrdinal("Longitude")).ToString(CultureInfo.InvariantCulture),
                ["Slug"] = Text(reader, "Slug")
            }));

    private static readonly IReadOnlyList<ImportType> SportDeetsTypes = [Venue];

    // ---- The run ----

    /// <returns>The process exit code: 1 when any row or gen-table refresh failed.</returns>
    private static async Task<int> ImportSportDeets(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // Main has already checked the target database (RequireSportDeetsDatabaseAsync), before migrating it.
        var logger = services.GetRequiredService<ILogger<Program>>();

        var producer = services.GetRequiredService<IConfiguration>().GetConnectionString(ProducerConnectionStringName)
            ?? throw new InvalidOperationException(
                $"ConnectionStrings:{ProducerConnectionStringName} is not set. Set it with: dotnet user-secrets set --id {SportDeetsSecretsId} " +
                $"\"ConnectionStrings:{ProducerConnectionStringName}\" \"Host=...;Database=sdProducer.FootballNcaa;Username=...;Password=...\"");

        var failed = 0;
        foreach (var type in SportDeetsTypes)
            failed += await ImportTypeAsync(services, type, producer, logger, cancellationToken);
        return failed == 0 ? 0 : 1;
    }

    /// <returns>The number of failures: rows not created, and gen-table builds or refreshes that failed.</returns>
    private static async Task<int> ImportTypeAsync(IServiceProvider services, ImportType type, string producer, ILogger logger, CancellationToken cancellationToken)
    {
        var rows = await ReadRowsAsync(producer, type, cancellationToken);
        logger.LogInformation("{Type}: read {Count} rows", type.Name, rows.Count);

        // Schema: pick lists, properties, the type, its definition, groups and the default view.
        var propertyIds = new Dictionary<string, Guid>(SystemProperties, StringComparer.OrdinalIgnoreCase);
        var pickListValues = new Dictionary<string, Dictionary<string, Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in type.Properties)
        {
            Guid? pickListId = null;
            if (property.DataType == DataType.PickList)
            {
                var values = property.FixedValues ?? rows
                    .Select(r => r.Values.GetValueOrDefault(property.Name)?.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Select(v => v!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var (listId, valueIds) = await EnsurePickListAsync(services, property.PickList ?? property.Name, values, logger, cancellationToken);
                pickListId = listId;
                pickListValues[property.Name] = valueIds;
            }
            propertyIds[property.Name] = await EnsurePropertyAsync(services, property, pickListId, logger, cancellationToken);
        }

        var (typeId, typeGenFailures) = await EnsureAssetTypeAsync(services, type, type.Properties.Select(p => propertyIds[p.Name]).ToList(), logger, cancellationToken);
        var definitionId = await EnsureDefinitionAsync(services, typeId, type.DefinitionName, logger, cancellationToken);
        var groupIds = new List<Guid>();
        foreach (var group in type.Groups)
            groupIds.Add(await EnsureGroupAsync(services, typeId, group, propertyIds, logger, cancellationToken));
        await EnsureViewAsync(services, typeId, type, groupIds, logger, cancellationToken);

        // Rows: one instance each, keeping the sportDeets id as the asset id.
        var rowIds = rows.Select(r => r.Id).ToList();
        var existing = await InScopeAsync(services, sp => sp.GetRequiredService<XdbDataContext>().Assets.AsNoTracking()
            .Where(a => rowIds.Contains(a.Id)).Select(a => a.Id).ToListAsync(cancellationToken));
        var skip = existing.ToHashSet();

        int created = 0, failed = 0, genFailures = typeGenFailures;
        foreach (var row in rows)
        {
            if (skip.Contains(row.Id))
                continue;

            var values = new List<CreateAssetValue>();
            foreach (var (name, text) in row.Values)
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                var value = pickListValues.TryGetValue(name, out var map) ? map[text.Trim()].ToString("D") : text;
                values.Add(new CreateAssetValue(propertyIds[name], value));
            }

            var result = await InScopeAsync(services, sp => sp.GetRequiredService<ICreateAssetCommandHandler>().ExecuteAsync(new CreateAssetCommand
            {
                ActorId = SystemMemberIds.Admin,
                Id = row.Id,
                AssetTypeId = typeId,
                Name = row.Name,
                InstanceOfId = definitionId,
                Values = values
            }, cancellationToken));

            if (result is Failure<CreateAssetResult> failure)
            {
                failed++;
                logger.LogError("{Type} {Id} '{Name}' failed: {Errors}", type.Name, row.Id, row.Name, Describe(failure.Errors));
                continue;
            }
            foreach (var genFailure in result.Value.Failures)
            {
                genFailures++;
                logger.LogError("{Type} {Id}: gen-table refresh failed: {Error}", type.Name, row.Id, genFailure.Error);
            }
            if (++created % 100 == 0)
                logger.LogInformation("{Type}: {Created} created", type.Name, created);
        }

        logger.LogInformation("{Type}: {Created} created, {Skipped} already present, {Failed} failed", type.Name, created, skip.Count, failed);
        // A rerun skips assets that already exist, so it cannot repair their gen-table rows; a rebuild does.
        if (genFailures > 0)
            logger.LogError("{Type}: {Count} gen-table failures; the assets exist but are missing from the gen tables. Rebuild them with " +
                "the {Verb} verb in the SportDeets environment.", type.Name, genFailures, RegenerateAllVerb);
        return failed + genFailures;
    }

    private static async Task<List<ImportRow>> ReadRowsAsync(string producer, ImportType type, CancellationToken cancellationToken)
    {
        var rows = new List<ImportRow>();
        await using var connection = new NpgsqlConnection(producer);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(type.Query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(type.Map(reader));
        return rows;
    }

    // ---- Find or create ----

    /// <returns>The list's id and its live values by text (case-insensitive).</returns>
    private static async Task<(Guid Id, Dictionary<string, Guid> Values)> EnsurePickListAsync(IServiceProvider services, string name,
        IReadOnlyList<string> values, ILogger logger, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var listId = await db.PickLists.AsNoTracking().Where(l => l.Name == name && l.Deleted == null).Select(l => (Guid?)l.Id).SingleOrDefaultAsync(cancellationToken);

        if (listId is null)
        {
            var result = await scope.ServiceProvider.GetRequiredService<ICreatePickListCommandHandler>().ExecuteAsync(new CreatePickListCommand
            {
                ActorId = SystemMemberIds.Admin,
                Name = name,
                Description = "Imported from sportDeets",
                Values = values.Select(v => new CreatePickListValue { Value = v }).ToList()
            }, cancellationToken);
            listId = Require(result, $"pick list '{name}'");
            logger.LogInformation("Created pick list '{Name}' with {Count} values", name, values.Count);
        }
        else
        {
            // A later export can bring a value the list lacks. No CreatePickListValue slice yet, so it is written here.
            var current = await db.PickListValues.Where(v => v.PickListId == listId && v.Deleted == null).ToListAsync(cancellationToken);
            var missing = values.Where(v => !current.Any(c => string.Equals(c.Value, v, StringComparison.OrdinalIgnoreCase))).ToList();
            if (missing.Count > 0)
            {
                var now = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().UtcNow();
                var order = current.Count == 0 ? 0 : current.Max(v => v.Order) + 1;
                foreach (var value in missing)
                {
                    db.PickListValues.Add(new PickListValue
                    {
                        Id = Guid.NewGuid(), PickListId = listId.Value, Value = value, Order = order++,
                        Created = now, CreatedBy = SystemMemberIds.Admin, Approved = now, ApprovedBy = SystemMemberIds.Admin
                    });
                }
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Added {Count} values to pick list '{Name}': {Values}", missing.Count, name, string.Join(", ", missing));
            }
        }

        var ids = await db.PickListValues.AsNoTracking().Where(v => v.PickListId == listId && v.Deleted == null)
            .Select(v => new { v.Id, v.Value }).ToListAsync(cancellationToken);
        return (listId.Value, ids.GroupBy(v => v.Value, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Properties are global: a live one with the same name is reused when its data type (and list) agree.</summary>
    private static async Task<Guid> EnsurePropertyAsync(IServiceProvider services, ImportProperty property, Guid? pickListId, ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var matches = await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Properties.AsNoTracking()
            .Where(p => p.Name == property.Name && p.Deleted == null && !p.IsSystem)
            .Select(p => new { p.Id, p.DataTypeId, p.PickListId })
            .ToListAsync(cancellationToken);

        switch (matches.Count)
        {
            case 1 when (DataType)matches[0].DataTypeId == property.DataType && matches[0].PickListId == pickListId:
                return matches[0].Id;
            case 1:
                throw new InvalidOperationException($"Property '{property.Name}' exists as {(DataType)matches[0].DataTypeId} " +
                    $"(pick list {matches[0].PickListId?.ToString() ?? "none"}); the import wants {property.DataType} (pick list {pickListId?.ToString() ?? "none"}).");
            case > 1:
                throw new InvalidOperationException($"{matches.Count} live properties are named '{property.Name}'; the import cannot tell which to use.");
        }

        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyCommandHandler>().ExecuteAsync(new CreatePropertyCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = property.Name,
            DataType = property.DataType,
            PickListId = pickListId
        }, cancellationToken);
        var id = Require(result, $"property '{property.Name}'");
        logger.LogInformation("Created property '{Name}' ({DataType})", property.Name, property.DataType);
        return id;
    }

    /// <summary>The type, matched by name; all imported properties are instance-level.</summary>
    private static async Task<(Guid Id, int GenFailures)> EnsureAssetTypeAsync(IServiceProvider services, ImportType type, IReadOnlyList<Guid> propertyIds, ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var existing = await db.AssetTypes.AsNoTracking().Where(t => t.Name == type.Name).Select(t => new { t.Id, t.Deleted }).SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (existing.Deleted is not null)
                throw new InvalidOperationException($"Asset type '{type.Name}' exists but is deleted; type names stay taken. Rerun with {ResetOption}.");

            // No slice adds links to an existing type yet: a property added to the import after the type was created needs a reset.
            var linked = await db.AssetTypesProperties.AsNoTracking()
                .Where(l => l.AssetTypeId == existing.Id && l.IsInstance && l.Deleted == null).Select(l => l.PropertyId).ToListAsync(cancellationToken);
            var unlinked = propertyIds.Except(linked).Count();
            if (unlinked > 0)
                logger.LogWarning("Asset type '{Name}' lacks {Count} of the import's properties; their values will fail. Rerun with {Reset}.",
                    type.Name, unlinked, ResetOption);
            return (existing.Id, 0);
        }

        var result = await scope.ServiceProvider.GetRequiredService<ICreateAssetTypeCommandHandler>().ExecuteAsync(new CreateAssetTypeCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = type.Name,
            Pluralization = type.Pluralization,
            DefinitionLabel = type.DefinitionLabel,
            InstanceLabel = type.InstanceLabel,
            Properties = propertyIds.Select(id => new CreateAssetTypeProperty(id, IsInstance: true)).ToList()
        }, cancellationToken);
        var created = Require(result, $"asset type '{type.Name}'");
        foreach (var failure in created.Failures)
            logger.LogError("Asset type '{Name}': gen-table generation failed: {Error}", type.Name, failure.Error);
        logger.LogInformation("Created asset type '{Name}' ({GenTables} gen tables)", type.Name, created.GenTables);
        return (created.Id, created.Failures.Count);
    }

    private static async Task<Guid> EnsureDefinitionAsync(IServiceProvider services, Guid typeId, string name, ILogger logger, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var existing = await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Assets.AsNoTracking()
            .Where(a => a.AssetTypeId == typeId && !a.IsInstance && a.Name == name && a.Deleted == null)
            .Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        if (existing is { } id)
            return id;

        var result = await scope.ServiceProvider.GetRequiredService<ICreateAssetCommandHandler>().ExecuteAsync(new CreateAssetCommand
        {
            ActorId = SystemMemberIds.Admin,
            AssetTypeId = typeId,
            Name = name
        }, cancellationToken);
        var created = Require(result, $"definition '{name}'");
        logger.LogInformation("Created definition '{Name}'", name);
        return created.Id;
    }

    private static async Task<Guid> EnsureGroupAsync(IServiceProvider services, Guid typeId, ImportGroup group, IReadOnlyDictionary<string, Guid> propertyIds,
        ILogger logger, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var existing = await scope.ServiceProvider.GetRequiredService<XdbDataContext>().PropertyGroups.AsNoTracking()
            .Where(g => g.AssetTypeId == typeId && g.IsInstance && g.Name == group.Name && g.Deleted == null)
            .Select(g => (Guid?)g.Id).SingleOrDefaultAsync(cancellationToken);
        if (existing is { } id)
            return id;

        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyGroupCommandHandler>().ExecuteAsync(new CreatePropertyGroupCommand
        {
            ActorId = SystemMemberIds.Admin,
            AssetTypeId = typeId,
            IsInstance = true,
            Name = group.Name,
            Properties = group.Properties
                .Select(name => new CreatePropertyGroupProperty(propertyIds[name], IsRequired: propertyIds[name] == SystemPropertyIds.AssetName))
                .ToList()
        }, cancellationToken);
        var created = Require(result, $"property group '{group.Name}'");
        logger.LogInformation("Created property group '{Name}'", group.Name);
        return created;
    }

    /// <summary>The instance view, made the type's default instance view when created.</summary>
    private static async Task EnsureViewAsync(IServiceProvider services, Guid typeId, ImportType type, IReadOnlyList<Guid> groupIds, ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        if (await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Views.AsNoTracking()
                .AnyAsync(v => v.AssetTypeId == typeId && v.Name == type.ViewName && v.Deleted == null, cancellationToken))
            return;

        var result = await scope.ServiceProvider.GetRequiredService<ICreateViewCommandHandler>().ExecuteAsync(new CreateViewCommand
        {
            ActorId = SystemMemberIds.Admin,
            AssetTypeId = typeId,
            IsInstance = true,
            Name = type.ViewName,
            Description = $"{type.Pluralization} imported from sportDeets",
            DriverCaption = type.InstanceLabel,
            NewItemCaption = $"Create {type.InstanceLabel}",
            PropertyGroupIds = groupIds,
            IsDefault = true
        }, cancellationToken);
        Require(result, $"view '{type.ViewName}'");
        logger.LogInformation("Created view '{Name}'", type.ViewName);
    }

    // ---- Reset and helpers ----

    /// <summary>Drops the import database; migrations and setup then rebuild it as a fresh install.</summary>
    private static async Task ResetSportDeetsDatabaseAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        RequireSportDeetsDatabase(db);
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogWarning("Dropping database {Database}", SportDeetsDatabase);
        await db.Database.EnsureDeletedAsync();
    }

    private static async Task RequireSportDeetsDatabaseAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        RequireSportDeetsDatabase(scope.ServiceProvider.GetRequiredService<XdbDataContext>());
    }

    /// <summary>Guards against importing into (or dropping) any other database, such as a copy of the original.</summary>
    private static void RequireSportDeetsDatabase(XdbDataContext db)
    {
        var database = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog;
        if (!string.Equals(database, SportDeetsDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"The sportDeets import runs only against {SportDeetsDatabase}, not '{database}'. Use the SportDeets environment.");
    }

    private static async Task<T> InScopeAsync<T>(IServiceProvider services, Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    private static T Require<T>(Result<T> result, string what) => result is Failure<T> failure
        ? throw new InvalidOperationException($"Creating {what} failed ({failure.Status}): {Describe(failure.Errors)}")
        : result.Value;

    private static string Describe(IEnumerable<FluentValidation.Results.ValidationFailure> errors) =>
        string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

    private static string? Text(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();
    }
}
