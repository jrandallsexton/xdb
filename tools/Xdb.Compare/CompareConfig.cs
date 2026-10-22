using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Xdb.Compare;

/// <summary>
/// Harness configuration. Lives OUTSIDE the repository: it names local services and databases,
/// and the captures it points at contain real data.
/// </summary>
public sealed class CompareConfig
{
    /// <summary>Base URL of the original service, up to and including "Service.svc".</summary>
    public required string LegacyBaseUrl { get; init; }

    /// <summary>Base URL of the port's legacy wire surface, up to and including "Service.svc".</summary>
    public required string PortBaseUrl { get; init; }

    /// <summary>SQL Server instance holding the databases below (integrated security).</summary>
    public string? SqlServer { get; init; }

    /// <summary>Databases snapshotted and reverted together so both sides start from the same data.</summary>
    public IReadOnlyList<string> Databases { get; init; } = [];

    /// <summary>Directory for snapshot data files (defaults to the server's data directory).</summary>
    public string? SnapshotDirectory { get; init; }

    /// <summary>Per-operation JSON paths whose values are not compared (e.g. timings). "*" applies to all operations.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Ignore { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Operations not replayed, e.g. ones that are random by design (GenerateGuid).</summary>
    public IReadOnlyList<string> Skip { get; init; } = [];

    public static CompareConfig Load(string path)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
        return JsonSerializer.Deserialize<CompareConfig>(File.ReadAllText(path), options)
            ?? throw new InvalidDataException($"Empty config: {path}");
    }

    /// <summary>Per-operation body masks (regex with a group named "mask"); see BodyMasks. "*" applies to all operations.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Masks { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Per-operation array paths compared as multisets (results whose order the original does not guarantee).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Unordered { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    public IReadOnlyList<string> IgnoredPathsFor(string operation) => For(Ignore, operation);

    public IReadOnlyList<string> UnorderedPathsFor(string operation) => For(Unordered, operation);

    public IReadOnlyList<string> MasksFor(string operation) => For(Masks, operation);

    private static List<string> For(IReadOnlyDictionary<string, IReadOnlyList<string>> map, string operation)
    {
        var values = new List<string>();
        if (map.TryGetValue("*", out var all))
            values.AddRange(all);
        if (map.TryGetValue(operation, out var specific))
            values.AddRange(specific);
        return values;
    }
}
