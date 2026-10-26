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
/// The sportDeets import (docs/13-import-venue.md), run with <c>dotnet run -- import-sportdeets [--reset]</c> under
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
    /// <param name="TargetType">For Asset properties: the name of the type referred to, imported earlier.</param>
    /// <param name="TargetIsInstance">For Asset properties: whether the values are that type's instances or its definitions.</param>
    private sealed record ImportProperty(string Name, DataType DataType, string? PickList = null, IReadOnlyList<string>? FixedValues = null,
        string? TargetType = null, bool TargetIsInstance = false);

    /// <param name="Properties">Import property names, or a key of <see cref="SystemProperties"/>.</param>
    private sealed record ImportGroup(string Name, IReadOnlyList<string> Properties);

    /// <param name="Values">
    /// Property name to text; null or blank writes no value. Pick list values are given as their text, Asset values as the
    /// referenced row's sportDeets id (assets keep that id).
    /// </param>
    /// <param name="DefinitionId">For an instance row: the sportDeets id of its definition's row.</param>
    private sealed record ImportRow(Guid Id, string Name, IReadOnlyDictionary<string, string?> Values, Guid? DefinitionId = null);

    /// <summary>One level of a type: its properties, groups and default view, and the Producer rows that become its assets.</summary>
    /// <param name="Label">The level's label (the type's DefinitionLabel or InstanceLabel); also the view's driver caption.</param>
    private sealed record ImportLevel(
        string Label,
        IReadOnlyList<ImportProperty> Properties,
        IReadOnlyList<ImportGroup> Groups,
        string ViewName,
        string Query,
        Func<DbDataReader, ImportRow> Map);

    /// <summary>
    /// One sportDeets entity as one asset type. Definitions are the lasting identities (a venue, a franchise); instances,
    /// for a type that has them, are occurrences of one (a franchise's seasons). A type without instances is definition-only.
    /// </summary>
    private sealed record ImportType(string Name, string Pluralization, ImportLevel Definitions, ImportLevel? Instances = null);

    // ---- What to import ----

    private static readonly IReadOnlyList<string> YesNo = ["Yes", "No"];

    private static readonly ImportType Venue = new(
        Name: "Venue",
        Pluralization: "Venues",
        Definitions: new ImportLevel(
            Label: "Venue",
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
                new("Facility", ["AssetName", "Short Name", "Surface", "Setting", "Capacity"]),
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
                    ["Latitude"] = Number(reader, "Latitude"),
                    ["Longitude"] = Number(reader, "Longitude"),
                    ["Slug"] = Text(reader, "Slug")
                })));

    private static readonly ImportType Franchise = new(
        Name: "Franchise",
        Pluralization: "Franchises",
        Definitions: new ImportLevel(
            Label: "Franchise",
            Properties:
            [
                new("Team Name", DataType.String),
                new("Location", DataType.String),
                new("Short Name", DataType.String),
                new("Abbreviation", DataType.String),
                new("Color", DataType.String),
                new("Alternate Color", DataType.String),
                new("Active", DataType.PickList, PickList: "Yes/No", FixedValues: YesNo),
                new("Home Venue", DataType.Asset, TargetType: "Venue"),
                new("Slug", DataType.String)
            ],
            Groups:
            [
                new("Franchise", ["AssetName", "Team Name", "Location", "Short Name", "Abbreviation", "Active"]),
                new("Colors", ["Color", "Alternate Color"]),
                new("Home", ["Home Venue"]),
                new("sportDeets", ["Slug"])
            ],
            ViewName: "Franchises",
            Query: """
                SELECT "Id", "DisplayName", "Name", "Location", "DisplayNameShort", "Abbreviation",
                       "ColorCodeHex", "ColorCodeAltHex", "IsActive", "VenueId", "Slug"
                FROM public."Franchise"
                ORDER BY "DisplayName"
                """,
            Map: reader => new ImportRow(
                reader.GetGuid(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("DisplayName")).Trim(),
                new Dictionary<string, string?>
                {
                    ["Team Name"] = Text(reader, "Name"),
                    ["Location"] = Text(reader, "Location"),
                    ["Short Name"] = Text(reader, "DisplayNameShort"),
                    ["Abbreviation"] = Text(reader, "Abbreviation"),
                    ["Color"] = Text(reader, "ColorCodeHex"),
                    ["Alternate Color"] = Text(reader, "ColorCodeAltHex"),
                    ["Active"] = reader.GetBoolean(reader.GetOrdinal("IsActive")) ? "Yes" : "No",
                    // An all-zero VenueId (179 rows) means none.
                    ["Home Venue"] = Reference(reader, "VenueId"),
                    ["Slug"] = Text(reader, "Slug")
                })),
        Instances: new ImportLevel(
            Label: "Season",
            Properties:
            [
                new("Season Year", DataType.Int),
                new("Classification", DataType.PickList, PickList: "Classification"),
                new("Conference", DataType.PickList, PickList: "Conference"),
                new("Conference Division", DataType.PickList, PickList: "Conference Division"),
                new("Venue", DataType.Asset, TargetType: "Venue"),
                new("All-Star", DataType.PickList, PickList: "Yes/No", FixedValues: YesNo),
                new("Wins", DataType.Int),
                new("Losses", DataType.Int),
                new("Ties", DataType.Int),
                new("Conference Wins", DataType.Int),
                new("Conference Losses", DataType.Int),
                new("Conference Ties", DataType.Int),
                new("Points Scored Avg", DataType.Float),
                new("Points Scored Max", DataType.Int),
                new("Points Scored Min", DataType.Int),
                new("Points Allowed Avg", DataType.Float),
                new("Points Allowed Max", DataType.Int),
                new("Points Allowed Min", DataType.Int),
                new("Win Margin Avg", DataType.Float),
                new("Win Margin Max", DataType.Int),
                new("Win Margin Min", DataType.Int),
                new("Loss Margin Avg", DataType.Float),
                new("Loss Margin Max", DataType.Int),
                new("Loss Margin Min", DataType.Int)
            ],
            Groups:
            [
                new("Season", ["AssetName", "InstanceOf", "Season Year", "Classification", "Conference", "Conference Division", "Venue", "All-Star"]),
                new("Record", ["Wins", "Losses", "Ties", "Conference Wins", "Conference Losses", "Conference Ties"]),
                new("Scoring", ["Points Scored Avg", "Points Scored Max", "Points Scored Min", "Points Allowed Avg", "Points Allowed Max",
                    "Points Allowed Min", "Win Margin Avg", "Win Margin Max", "Win Margin Min", "Loss Margin Avg", "Loss Margin Max", "Loss Margin Min"])
            ],
            ViewName: "Franchise Seasons",
            // The season's group and its ancestors, nearest first; SeasonGroups picks the conference and classification out of them.
            Query: """
                WITH RECURSIVE chain AS (
                    SELECT s."Id" AS "SeasonId", g."ParentId", g."Name", g."IsConference", 0 AS "Depth"
                    FROM public."FranchiseSeason" s
                    JOIN public."GroupSeason" g ON g."Id" = s."GroupSeasonId"
                    UNION ALL
                    SELECT c."SeasonId", p."ParentId", p."Name", p."IsConference", c."Depth" + 1
                    FROM chain c
                    JOIN public."GroupSeason" p ON p."Id" = c."ParentId"
                    WHERE c."Depth" < 10
                ), groups AS (
                    SELECT "SeasonId", array_agg("Name" ORDER BY "Depth") AS "GroupNames", array_agg("IsConference" ORDER BY "Depth") AS "GroupIsConference"
                    FROM chain
                    GROUP BY "SeasonId"
                )
                SELECT s."Id", s."FranchiseId", s."SeasonYear", s."DisplayName", s."VenueId", s."IsAllStar",
                       s."Wins", s."Losses", s."Ties", s."ConferenceWins", s."ConferenceLosses", s."ConferenceTies",
                       s."PtsScoredAvg", s."PtsScoredMax", s."PtsScoredMin", s."PtsAllowedAvg", s."PtsAllowedMax", s."PtsAllowedMin",
                       s."MarginWinAvg", s."MarginWinMax", s."MarginWinMin", s."MarginLossAvg", s."MarginLossMax", s."MarginLossMin",
                       g."GroupNames", g."GroupIsConference"
                FROM public."FranchiseSeason" s
                LEFT JOIN groups g ON g."SeasonId" = s."Id"
                ORDER BY s."SeasonYear", s."DisplayName"
                """,
            Map: reader =>
            {
                var groups = SeasonGroups(reader);
                return new ImportRow(
                    reader.GetGuid(reader.GetOrdinal("Id")),
                    $"{reader.GetInt32(reader.GetOrdinal("SeasonYear"))} {reader.GetString(reader.GetOrdinal("DisplayName")).Trim()}",
                    new Dictionary<string, string?>
                    {
                        ["Season Year"] = Number(reader, "SeasonYear"),
                        ["Classification"] = groups.Classification,
                        ["Conference"] = groups.Conference,
                        ["Conference Division"] = groups.Division,
                        ["Venue"] = Reference(reader, "VenueId"),
                        ["All-Star"] = reader.GetBoolean(reader.GetOrdinal("IsAllStar")) ? "Yes" : "No",
                        ["Wins"] = Number(reader, "Wins"),
                        ["Losses"] = Number(reader, "Losses"),
                        ["Ties"] = Number(reader, "Ties"),
                        ["Conference Wins"] = Number(reader, "ConferenceWins"),
                        ["Conference Losses"] = Number(reader, "ConferenceLosses"),
                        ["Conference Ties"] = Number(reader, "ConferenceTies"),
                        ["Points Scored Avg"] = Number(reader, "PtsScoredAvg"),
                        ["Points Scored Max"] = Number(reader, "PtsScoredMax"),
                        ["Points Scored Min"] = Number(reader, "PtsScoredMin"),
                        ["Points Allowed Avg"] = Number(reader, "PtsAllowedAvg"),
                        ["Points Allowed Max"] = Number(reader, "PtsAllowedMax"),
                        ["Points Allowed Min"] = Number(reader, "PtsAllowedMin"),
                        ["Win Margin Avg"] = Number(reader, "MarginWinAvg"),
                        ["Win Margin Max"] = Number(reader, "MarginWinMax"),
                        ["Win Margin Min"] = Number(reader, "MarginWinMin"),
                        ["Loss Margin Avg"] = Number(reader, "MarginLossAvg"),
                        ["Loss Margin Max"] = Number(reader, "MarginLossMax"),
                        ["Loss Margin Min"] = Number(reader, "MarginLossMin")
                    },
                    DefinitionId: reader.GetGuid(reader.GetOrdinal("FranchiseId")));
            }));

    /// <summary>
    /// Athlete seasons imported, by FranchiseSeason year: the NCAAFB tables hold about 625k seasons of 252k athletes, more
    /// than the per-asset write path should take in one run. Athletes are those with a season in the window.
    /// </summary>
    private const int AthleteSeasonsFrom = 2025, AthleteSeasonsTo = 2025;

    /// <summary>The athletes with a season in the window, with their birth location; read for Person and for Football Athlete.</summary>
    private static readonly string AthletesInWindowQuery = $"""
        SELECT a."Id", a."DisplayName", a."FirstName", a."LastName", a."ShortName", a."DoB", a."HeightIn", a."WeightLb", a."Slug",
               l."City", l."State", l."Country"
        FROM public."Athlete" a
        LEFT JOIN public."Location" l ON l."Id" = a."BirthLocationId"
        WHERE EXISTS (
            SELECT 1 FROM public."AthleteSeason" s
            JOIN public."FranchiseSeason" fs ON fs."Id" = s."FranchiseSeasonId"
            WHERE s."AthleteId" = a."Id" AND fs."SeasonYear" BETWEEN {AthleteSeasonsFrom} AND {AthleteSeasonsTo})
        ORDER BY a."DisplayName"
        """;

    /// <summary>The people behind the athletes in the window and behind every coach; Role says which, for the Person id.</summary>
    private static readonly string PeopleQuery = $"""
        SELECT 'Athlete' AS "Role", a."Id", a."DisplayName", a."FirstName", a."LastName", a."DoB", a."HeightIn", a."WeightLb",
               l."City", l."State", l."Country"
        FROM public."Athlete" a
        LEFT JOIN public."Location" l ON l."Id" = a."BirthLocationId"
        WHERE EXISTS (
            SELECT 1 FROM public."AthleteSeason" s
            JOIN public."FranchiseSeason" fs ON fs."Id" = s."FranchiseSeasonId"
            WHERE s."AthleteId" = a."Id" AND fs."SeasonYear" BETWEEN {AthleteSeasonsFrom} AND {AthleteSeasonsTo})
        UNION ALL
        SELECT 'Coach', c."Id", concat_ws(' ', c."FirstName", c."LastName"), c."FirstName", c."LastName", c."DateOfBirth", NULL, NULL,
               NULL, NULL, NULL
        FROM public."Coach" c
        ORDER BY 3
        """;

    /// <summary>
    /// The human, whatever roles they have: a football athlete and a coach each refer to one, so a coach can be linked to
    /// their playing career. The Producer does not link athletes and coaches yet, so for now each athlete and each coach
    /// brings a Person of their own; when that link exists (from the Producer or curated here), roles share one Person.
    /// </summary>
    private static readonly ImportType Person = new(
        Name: "Person",
        Pluralization: "People",
        Definitions: new ImportLevel(
            Label: "Person",
            Properties:
            [
                new("First Name", DataType.String),
                new("Last Name", DataType.String),
                new("Birth Date", DataType.Date),
                new("Birth City", DataType.String),
                // The venues' lists: a state's related items then show its venues and the people born there.
                new("Birth State", DataType.PickList, PickList: "State"),
                new("Birth Country", DataType.PickList, PickList: "Country"),
                new("Height (in)", DataType.Float),
                new("Weight (lb)", DataType.Float)
            ],
            Groups:
            [
                new("Person", ["AssetName", "First Name", "Last Name", "Birth Date"]),
                new("Birthplace", ["Birth City", "Birth State", "Birth Country"]),
                new("Physical", ["Height (in)", "Weight (lb)"])
            ],
            ViewName: "People",
            Query: PeopleQuery,
            Map: reader => new ImportRow(
                PersonId(reader.GetString(reader.GetOrdinal("Role")), reader.GetGuid(reader.GetOrdinal("Id"))),
                reader.GetString(reader.GetOrdinal("DisplayName")).Trim(),
                new Dictionary<string, string?>
                {
                    ["First Name"] = Text(reader, "FirstName"),
                    ["Last Name"] = Text(reader, "LastName"),
                    ["Birth Date"] = reader.IsDBNull(reader.GetOrdinal("DoB"))
                        ? null
                        : reader.GetFieldValue<DateTime>(reader.GetOrdinal("DoB")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["Birth City"] = Text(reader, "City"),
                    ["Birth State"] = Text(reader, "State"),
                    ["Birth Country"] = Text(reader, "Country"),
                    // 0 is unknown, as with venue capacity.
                    ["Height (in)"] = Positive(reader, "HeightIn"),
                    ["Weight (lb)"] = Positive(reader, "WeightLb")
                })));

    private static readonly ImportType FootballAthlete = new(
        Name: "Football Athlete",
        Pluralization: "Football Athletes",
        Definitions: new ImportLevel(
            Label: "Athlete",
            Properties:
            [
                new("Person", DataType.Asset, TargetType: "Person"),
                new("Short Name", DataType.String),
                new("Slug", DataType.String)
            ],
            Groups:
            [
                new("Athlete", ["AssetName", "Person", "Short Name"]),
                new("sportDeets", ["Slug"])
            ],
            ViewName: "Football Athletes",
            Query: AthletesInWindowQuery,
            Map: reader => new ImportRow(
                reader.GetGuid(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("DisplayName")).Trim(),
                new Dictionary<string, string?>
                {
                    ["Person"] = PersonId("Athlete", reader.GetGuid(reader.GetOrdinal("Id"))).ToString("D"),
                    ["Short Name"] = Text(reader, "ShortName"),
                    ["Slug"] = Text(reader, "Slug")
                })),
        Instances: new ImportLevel(
            Label: "Season",
            Properties:
            [
                new("Season Year", DataType.Int),
                new("Team", DataType.Asset, TargetType: "Franchise", TargetIsInstance: true),
                new("Position", DataType.PickList, PickList: "Position"),
                new("Jersey", DataType.String),
                new("Class", DataType.PickList, PickList: "Class"),
                new("Status", DataType.PickList, PickList: "Athlete Status"),
                new("Active", DataType.PickList, PickList: "Yes/No", FixedValues: YesNo)
            ],
            Groups:
            [
                new("Season", ["AssetName", "InstanceOf", "Season Year", "Team", "Position", "Jersey", "Class", "Status", "Active"])
            ],
            ViewName: "Football Athlete Seasons",
            Query: $"""
                SELECT s."Id", s."AthleteId", s."FranchiseSeasonId", fs."SeasonYear", s."DisplayName", p."DisplayName" AS "Position",
                       s."Jersey", s."ExperienceDisplayValue", st."Name" AS "Status", s."IsActive"
                FROM public."AthleteSeason" s
                JOIN public."FranchiseSeason" fs ON fs."Id" = s."FranchiseSeasonId"
                LEFT JOIN public."AthletePosition" p ON p."Id" = s."PositionId"
                LEFT JOIN public."AthleteStatus" st ON st."Id" = s."StatusId"
                WHERE fs."SeasonYear" BETWEEN {AthleteSeasonsFrom} AND {AthleteSeasonsTo}
                ORDER BY fs."SeasonYear", s."DisplayName"
                """,
            Map: reader => new ImportRow(
                reader.GetGuid(reader.GetOrdinal("Id")),
                $"{reader.GetInt32(reader.GetOrdinal("SeasonYear"))} {reader.GetString(reader.GetOrdinal("DisplayName")).Trim()}",
                new Dictionary<string, string?>
                {
                    ["Season Year"] = Number(reader, "SeasonYear"),
                    ["Team"] = Reference(reader, "FranchiseSeasonId"),
                    ["Position"] = Text(reader, "Position"),
                    ["Jersey"] = Text(reader, "Jersey"),
                    ["Class"] = Text(reader, "ExperienceDisplayValue"),
                    ["Status"] = Text(reader, "Status"),
                    ["Active"] = reader.GetBoolean(reader.GetOrdinal("IsActive")) ? "Yes" : "No"
                },
                DefinitionId: reader.GetGuid(reader.GetOrdinal("AthleteId")))));

    /// <summary>
    /// A coach: definition = the coach (a Person), instance = a season with a team. Not imported: CoachSeason.Title, which
    /// holds the coach's last name on every row (a Producer mapping bug), and Coach.Title, empty on every row.
    /// </summary>
    private static readonly ImportType Coach = new(
        Name: "Coach",
        Pluralization: "Coaches",
        Definitions: new ImportLevel(
            Label: "Coach",
            Properties:
            [
                new("Person", DataType.Asset, TargetType: "Person"),
                new("Experience (years)", DataType.Int)
            ],
            Groups:
            [
                new("Coach", ["AssetName", "Person", "Experience (years)"])
            ],
            ViewName: "Coaches",
            Query: """
                SELECT c."Id", concat_ws(' ', c."FirstName", c."LastName") AS "Name", c."Experience"
                FROM public."Coach" c
                ORDER BY 2
                """,
            Map: reader => new ImportRow(
                reader.GetGuid(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("Name")).Trim(),
                new Dictionary<string, string?>
                {
                    ["Person"] = PersonId("Coach", reader.GetGuid(reader.GetOrdinal("Id"))).ToString("D"),
                    // 0 is unknown (242 of 276 rows).
                    ["Experience (years)"] = Positive(reader, "Experience")
                })),
        Instances: new ImportLevel(
            Label: "Season",
            Properties:
            [
                new("Season Year", DataType.Int),
                new("Team", DataType.Asset, TargetType: "Franchise", TargetIsInstance: true),
                new("Active", DataType.PickList, PickList: "Yes/No", FixedValues: YesNo)
            ],
            Groups:
            [
                new("Season", ["AssetName", "InstanceOf", "Season Year", "Team", "Active"])
            ],
            ViewName: "Coach Seasons",
            Query: """
                SELECT s."Id", s."CoachId", s."FranchiseSeasonId", s."IsActive", fs."SeasonYear",
                       concat_ws(' ', c."FirstName", c."LastName") AS "Name"
                FROM public."CoachSeason" s
                JOIN public."Coach" c ON c."Id" = s."CoachId"
                JOIN public."FranchiseSeason" fs ON fs."Id" = s."FranchiseSeasonId"
                ORDER BY fs."SeasonYear", 6
                """,
            Map: reader => new ImportRow(
                reader.GetGuid(reader.GetOrdinal("Id")),
                $"{reader.GetInt32(reader.GetOrdinal("SeasonYear"))} {reader.GetString(reader.GetOrdinal("Name")).Trim()}",
                new Dictionary<string, string?>
                {
                    ["Season Year"] = Number(reader, "SeasonYear"),
                    ["Team"] = Reference(reader, "FranchiseSeasonId"),
                    ["Active"] = reader.GetBoolean(reader.GetOrdinal("IsActive")) ? "Yes" : "No"
                },
                DefinitionId: reader.GetGuid(reader.GetOrdinal("CoachId")))));

    /// <summary>In order: a type's Asset properties refer to types listed before it.</summary>
    private static readonly IReadOnlyList<ImportType> SportDeetsTypes = [Venue, Franchise, Person, FootballAthlete, Coach];

    /// <summary>
    /// From a season's group chain (its group first, then ancestors): the nearest conference, the group below it (a
    /// conference's division, e.g. East/West), and the classification: the group above the conference, or the season's own
    /// group when no conference is in the chain (Division II/III, NAIA). Older classification names are folded into today's.
    /// </summary>
    private static (string? Conference, string? Division, string? Classification) SeasonGroups(DbDataReader reader)
    {
        var namesOrdinal = reader.GetOrdinal("GroupNames");
        if (reader.IsDBNull(namesOrdinal))
            return (null, null, null);
        var names = reader.GetFieldValue<string[]>(namesOrdinal);
        var isConference = reader.GetFieldValue<bool[]>(reader.GetOrdinal("GroupIsConference"));

        var conference = Array.IndexOf(isConference, true);
        var classification = (conference < 0 ? names[0] : names.ElementAtOrDefault(conference + 1)) switch
        {
            "FBS (I-A)" => "FBS",
            "FCS (I-AA)" => "FCS",
            var name => name
        };
        return conference < 0
            ? (null, null, classification)
            : (names[conference], conference > 0 ? names[conference - 1] : null, classification);
    }

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
        var definitionRows = await ReadRowsAsync(producer, type.Definitions, cancellationToken);
        var instanceRows = type.Instances is null ? [] : await ReadRowsAsync(producer, type.Instances, cancellationToken);
        logger.LogInformation("{Type}: read {Definitions} definition and {Instances} instance rows", type.Name, definitionRows.Count, instanceRows.Count);

        // A property linked at both levels of one type has two relations that some readers cannot tell apart.
        var bothLevels = type.Definitions.Properties.Select(p => p.Name)
            .Intersect(type.Instances?.Properties.Select(p => p.Name) ?? [], StringComparer.OrdinalIgnoreCase).ToList();
        if (bothLevels.Count > 0)
            throw new InvalidOperationException($"{type.Name}: properties at both levels: {string.Join(", ", bothLevels)}.");

        // Schema: pick lists, properties, the type, then each level's groups and default view.
        var propertyIds = new Dictionary<string, Guid>(SystemProperties, StringComparer.OrdinalIgnoreCase);
        var pickListValues = new Dictionary<string, Dictionary<string, Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (level, rows) in Levels(type, definitionRows, instanceRows))
        {
            foreach (var property in level.Properties)
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
        }

        var (typeId, genFailures) = await EnsureAssetTypeAsync(services, type,
            type.Definitions.Properties.Select(p => propertyIds[p.Name]).ToList(),
            type.Instances?.Properties.Select(p => propertyIds[p.Name]).ToList() ?? [],
            logger, cancellationToken);
        foreach (var (level, isInstance) in new[] { (type.Definitions, false), (type.Instances, true) })
        {
            if (level is null)
                continue;
            var groupIds = new List<Guid>();
            foreach (var group in level.Groups)
                groupIds.Add(await EnsureGroupAsync(services, typeId, isInstance, group, propertyIds, logger, cancellationToken));
            await EnsureViewAsync(services, typeId, type, level, isInstance, groupIds, logger, cancellationToken);
        }

        // Assets, keeping the sportDeets ids: definitions first, so instances can name theirs.
        var failed = 0;
        foreach (var (level, rows) in Levels(type, definitionRows, instanceRows))
        {
            var (levelFailed, levelGenFailures) = await CreateAssetsAsync(services, typeId, level, rows, propertyIds, pickListValues, logger, cancellationToken);
            failed += levelFailed;
            genFailures += levelGenFailures;
        }

        // A rerun skips assets that already exist, so it cannot repair their gen-table rows; a rebuild does.
        if (genFailures > 0)
            logger.LogError("{Type}: {Count} gen-table failures; the assets exist but are missing from the gen tables. Rebuild them with " +
                "the {Verb} verb in the SportDeets environment.", type.Name, genFailures, RegenerateAllVerb);
        return failed + genFailures;
    }

    private static IEnumerable<(ImportLevel Level, IReadOnlyList<ImportRow> Rows)> Levels(ImportType type,
        IReadOnlyList<ImportRow> definitionRows, IReadOnlyList<ImportRow> instanceRows)
    {
        yield return (type.Definitions, definitionRows);
        if (type.Instances is not null)
            yield return (type.Instances, instanceRows);
    }

    /// <returns>Rows that failed, and gen-table refreshes that failed for rows that were created.</returns>
    private static async Task<(int Failed, int GenFailures)> CreateAssetsAsync(IServiceProvider services, Guid typeId, ImportLevel level,
        IReadOnlyList<ImportRow> rows, IReadOnlyDictionary<string, Guid> propertyIds, IReadOnlyDictionary<string, Dictionary<string, Guid>> pickListValues,
        ILogger logger, CancellationToken cancellationToken)
    {
        var existing = new HashSet<Guid>();
        // Chunked: SQL Server takes at most 2100 parameters.
        foreach (var chunk in rows.Select(r => r.Id).Chunk(2000))
            existing.UnionWith(await InScopeAsync(services, sp => sp.GetRequiredService<XdbDataContext>().Assets.AsNoTracking()
                .Where(a => chunk.Contains(a.Id)).Select(a => a.Id).ToListAsync(cancellationToken)));

        int created = 0, failed = 0, genFailures = 0;
        foreach (var row in rows)
        {
            if (existing.Contains(row.Id))
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
                InstanceOfId = row.DefinitionId,
                Values = values
            }, cancellationToken));

            if (result is Failure<CreateAssetResult> failure)
            {
                failed++;
                logger.LogError("{Level} {Id} '{Name}' failed: {Errors}", level.Label, row.Id, row.Name, Describe(failure.Errors));
                continue;
            }
            foreach (var genFailure in result.Value.Failures)
            {
                genFailures++;
                logger.LogError("{Level} {Id}: gen-table refresh failed: {Error}", level.Label, row.Id, genFailure.Error);
            }
            if (++created % 500 == 0)
                logger.LogInformation("{Level}: {Created} created", level.Label, created);
        }

        logger.LogInformation("{Level}: {Created} created, {Skipped} already present, {Failed} failed", level.Label, created, existing.Count, failed);
        return (failed, genFailures);
    }

    private static async Task<List<ImportRow>> ReadRowsAsync(string producer, ImportLevel level, CancellationToken cancellationToken)
    {
        var rows = new List<ImportRow>();
        await using var connection = new NpgsqlConnection(producer);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(level.Query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(level.Map(reader));
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

    /// <summary>
    /// Properties are global: a live one with the same name is reused when its data type agrees, and its pick list or
    /// target (type and level) for PickList and Asset properties.
    /// </summary>
    private static async Task<Guid> EnsurePropertyAsync(IServiceProvider services, ImportProperty property, Guid? pickListId, ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();

        Guid? targetTypeId = null;
        bool? targetIsInstance = null;
        if (property.DataType == DataType.Asset)
        {
            var targetName = property.TargetType ?? throw new InvalidOperationException($"Asset property '{property.Name}' names no target type.");
            targetTypeId = await db.AssetTypes.AsNoTracking().Where(t => t.Name == targetName && t.Deleted == null)
                    .Select(t => (Guid?)t.Id).SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Asset property '{property.Name}' refers to type '{targetName}', which is not imported yet.");
            targetIsInstance = property.TargetIsInstance;
        }

        var matches = await db.Properties.AsNoTracking()
            .Where(p => p.Name == property.Name && p.Deleted == null && !p.IsSystem)
            .Select(p => new { p.Id, p.DataTypeId, p.PickListId, p.AssetTypeId, p.AssetTypeIsInstance })
            .ToListAsync(cancellationToken);

        switch (matches.Count)
        {
            case 1 when (DataType)matches[0].DataTypeId == property.DataType && matches[0].PickListId == pickListId
                        && matches[0].AssetTypeId == targetTypeId && matches[0].AssetTypeIsInstance == targetIsInstance:
                return matches[0].Id;
            case 1:
                throw new InvalidOperationException($"Property '{property.Name}' exists as {(DataType)matches[0].DataTypeId} " +
                    $"(pick list {matches[0].PickListId?.ToString() ?? "none"}, target {matches[0].AssetTypeId?.ToString() ?? "none"}); the import wants " +
                    $"{property.DataType} (pick list {pickListId?.ToString() ?? "none"}, target {targetTypeId?.ToString() ?? "none"}).");
            case > 1:
                throw new InvalidOperationException($"{matches.Count} live properties are named '{property.Name}'; the import cannot tell which to use.");
        }

        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyCommandHandler>().ExecuteAsync(new CreatePropertyCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = property.Name,
            DataType = property.DataType,
            PickListId = pickListId,
            AssetTypeId = targetTypeId,
            AssetTypeIsInstance = targetIsInstance
        }, cancellationToken);
        var id = Require(result, $"property '{property.Name}'");
        logger.LogInformation("Created property '{Name}' ({DataType})", property.Name, property.DataType);
        return id;
    }

    /// <summary>The type, matched by name, with each level's properties linked at that level.</summary>
    private static async Task<(Guid Id, int GenFailures)> EnsureAssetTypeAsync(IServiceProvider services, ImportType type,
        IReadOnlyList<Guid> definitionPropertyIds, IReadOnlyList<Guid> instancePropertyIds, ILogger logger, CancellationToken cancellationToken)
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
                .Where(l => l.AssetTypeId == existing.Id && l.Deleted == null).Select(l => new { l.PropertyId, l.IsInstance }).ToListAsync(cancellationToken);
            var unlinked = definitionPropertyIds.Count(id => !linked.Contains(new { PropertyId = id, IsInstance = false }))
                + instancePropertyIds.Count(id => !linked.Contains(new { PropertyId = id, IsInstance = true }));
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
            DefinitionLabel = type.Definitions.Label,
            // Null for a definition-only type: it has no instance level.
            InstanceLabel = type.Instances?.Label,
            Properties = definitionPropertyIds.Select(id => new CreateAssetTypeProperty(id, IsInstance: false))
                .Concat(instancePropertyIds.Select(id => new CreateAssetTypeProperty(id, IsInstance: true)))
                .ToList()
        }, cancellationToken);
        var created = Require(result, $"asset type '{type.Name}'");
        foreach (var failure in created.Failures)
            logger.LogError("Asset type '{Name}': gen-table generation failed: {Error}", type.Name, failure.Error);
        logger.LogInformation("Created asset type '{Name}' ({GenTables} gen tables)", type.Name, created.GenTables);
        return (created.Id, created.Failures.Count);
    }

    private static async Task<Guid> EnsureGroupAsync(IServiceProvider services, Guid typeId, bool isInstance, ImportGroup group,
        IReadOnlyDictionary<string, Guid> propertyIds, ILogger logger, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var existing = await scope.ServiceProvider.GetRequiredService<XdbDataContext>().PropertyGroups.AsNoTracking()
            .Where(g => g.AssetTypeId == typeId && g.IsInstance == isInstance && g.Name == group.Name && g.Deleted == null)
            .Select(g => (Guid?)g.Id).SingleOrDefaultAsync(cancellationToken);
        if (existing is { } id)
            return id;

        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyGroupCommandHandler>().ExecuteAsync(new CreatePropertyGroupCommand
        {
            ActorId = SystemMemberIds.Admin,
            AssetTypeId = typeId,
            IsInstance = isInstance,
            Name = group.Name,
            Properties = group.Properties
                .Select(name => new CreatePropertyGroupProperty(propertyIds[name], IsRequired: propertyIds[name] == SystemPropertyIds.AssetName))
                .ToList()
        }, cancellationToken);
        var created = Require(result, $"property group '{group.Name}'");
        logger.LogInformation("Created property group '{Name}'", group.Name);
        return created;
    }

    /// <summary>The level's view, made the type's default view for that level when created.</summary>
    private static async Task EnsureViewAsync(IServiceProvider services, Guid typeId, ImportType type, ImportLevel level, bool isInstance,
        IReadOnlyList<Guid> groupIds, ILogger logger, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        if (await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Views.AsNoTracking()
                .AnyAsync(v => v.AssetTypeId == typeId && v.Name == level.ViewName && v.Deleted == null, cancellationToken))
            return;

        var result = await scope.ServiceProvider.GetRequiredService<ICreateViewCommandHandler>().ExecuteAsync(new CreateViewCommand
        {
            ActorId = SystemMemberIds.Admin,
            AssetTypeId = typeId,
            IsInstance = isInstance,
            Name = level.ViewName,
            Description = $"{level.ViewName} imported from sportDeets",
            DriverCaption = level.Label,
            NewItemCaption = $"Create {level.Label}",
            PropertyGroupIds = groupIds,
            IsDefault = true
        }, cancellationToken);
        Require(result, $"view '{level.ViewName}' of {type.Name}");
        logger.LogInformation("Created view '{Name}'", level.ViewName);
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

    /// <summary>An int or numeric column as invariant text; NULL writes no value.</summary>
    private static string? Number(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The Person asset's id for a sportDeets athlete or coach. Their own id is already the role asset's (Football Athlete,
    /// Coach), so the Person gets a stable id derived from it (name-based, MD5): reruns find the same Person.
    /// </summary>
    private static Guid PersonId(string role, Guid sourceId)
    {
        // Athletes came first and keep their original key.
        var key = role == "Athlete" ? $"sportDeets/Person/{sourceId:D}" : $"sportDeets/Person/{role}/{sourceId:D}";
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    /// <summary>A positive number as invariant text; NULL or 0 (unknown in the Producer data) writes no value.</summary>
    private static string? Positive(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) || Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture) <= 0
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <summary>A reference to another imported row, by its sportDeets id (which its asset keeps); NULL or all-zero writes no value.</summary>
    private static string? Reference(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) || reader.GetGuid(ordinal) == Guid.Empty ? null : reader.GetGuid(ordinal).ToString("D");
    }
}
