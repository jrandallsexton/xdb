using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Compare.Comparison;
using Xdb.Compare.Har;
using Xdb.Compare.Snapshots;

namespace Xdb.Compare;

/// <summary>
/// Golden-master harness: replays recorded service calls against the original and the port and diffs the responses.
///   replay   --config file.json --har capture.har [--op Name]... [--recorded] [--show-values]
///   snapshot create|revert|drop --config file.json
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
            return Usage();

        var options = Options.Parse(args.Skip(1));
        var config = CompareConfig.Load(options.Required("config"));
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

        switch (args[0])
        {
            case "replay":
                return await ReplayAsync(config, options, cancellation.Token);

            case "snapshot":
                var snapshots = new DatabaseSnapshots(config.SqlServer ?? throw new ArgumentException("config: SqlServer is required"), config.SnapshotDirectory);
                switch (options.Positional.FirstOrDefault())
                {
                    case "create": await snapshots.CreateAsync(config.Databases, cancellation.Token); return 0;
                    case "revert": await snapshots.RevertAsync(config.Databases, cancellation.Token); return 0;
                    case "drop": await snapshots.DropAsync(config.Databases, cancellation.Token); return 0;
                    default: return Usage();
                }

            default:
                return Usage();
        }
    }

    private static async Task<int> ReplayAsync(CompareConfig config, Options options, CancellationToken cancellationToken)
    {
        var only = options.All("op").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var calls = HarReader.Read(options.Required("har"))
            .Where(c => only.Count == 0 || only.Contains(c.Operation))
            .Where(c => !config.Skip.Contains(c.Operation, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var againstRecorded = options.Flag("recorded");
        var showValues = options.Flag("show-values");
        var delay = TimeSpan.FromMilliseconds(int.Parse(options.All("delay-ms").LastOrDefault() ?? "0", System.Globalization.CultureInfo.InvariantCulture));

        var timeoutMinutes = options.All("timeout-min").LastOrDefault();
        using var replayer = new Replayer(timeoutMinutes is null
            ? null
            : TimeSpan.FromMinutes(int.Parse(timeoutMinutes, System.Globalization.CultureInfo.InvariantCulture)));
        var grades = new Dictionary<ComparisonGrade, int>();
        var deferred = 0;
        foreach (var call in calls)
        {
            // Background work started by a previous call (e.g. closing a report log) may need time to finish,
            // as it had between the user's clicks when the capture was recorded.
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);

            var legacy = againstRecorded
                ? CapturedResponse.FromText(call.RecordedStatus, call.RecordedContentType, call.RecordedBody)
                : await replayer.SendAsync(config.LegacyBaseUrl, call, cancellationToken);
            var port = await replayer.SendAsync(config.PortBaseUrl, call, cancellationToken);
            var result = ResponseComparer.Compare(legacy, port, new ComparisonOptions(
                new IgnoreRules(config.IgnoredPathsFor(call.Operation)),
                new BodyMasks(config.MasksFor(call.Operation)),
                new IgnoreRules(config.UnorderedPathsFor(call.Operation)),
                config.UnorderedXmlItems.Contains(call.Operation, StringComparer.OrdinalIgnoreCase),
                config.Xlsx.Contains(call.Operation, StringComparer.OrdinalIgnoreCase)));

            var grade = result.Grade.ToString();
            if (result.Grade == ComparisonGrade.Different
                && config.ExpectDifferent.FirstOrDefault(r => r.Matches(call.Operation, call.RelativeUrl)) is { } expected)
            {
                grade = $"Deferred ({expected.Reason})";
                deferred++;
            }
            else
            {
                grades[result.Grade] = grades.GetValueOrDefault(result.Grade) + 1;
            }
            Console.WriteLine($"#{call.Index,-4} {call.Method,-4} {call.Operation,-40} {grade}");
            foreach (var d in result.Differences.Take(20))
                Console.WriteLine($"       {(d.Ignored ? "(ignored) " : string.Empty)}{d.Kind} {d.Path}: {Show(d.Legacy, showValues)} -> {Show(d.Port, showValues)}");
            if (result.Differences.Count > 20)
                Console.WriteLine($"       ... {result.Differences.Count - 20} more");
        }

        Console.WriteLine();
        Console.WriteLine($"{calls.Count} calls: " + string.Join(", ", grades.OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Value}"))
            + (deferred > 0 ? $", Deferred {deferred}" : string.Empty));
        return grades.GetValueOrDefault(ComparisonGrade.Different) == 0 ? 0 : 1;
    }

    /// <summary>Captures hold real data: values are shown only on request; otherwise only their shape.</summary>
    private static string Show(string? value, bool showValues)
    {
        if (value is null)
            return "-";
        if (showValues)
            return value.Length > 200 ? value[..200] + "..." : value;
        return value.StartsWith('"') ? $"<string len={value.Length - 2}>" : $"<{(value.Length > 0 ? "token" : "empty")} len={value.Length}>";
    }

    private static int Usage()
    {
        Console.Error.WriteLine("usage:");
        Console.Error.WriteLine("  replay --config file.json --har capture.har [--op Name]... [--recorded] [--show-values] [--delay-ms N] [--timeout-min N]");
        Console.Error.WriteLine("  snapshot create|revert|drop --config file.json");
        return 2;
    }

    private sealed class Options
    {
        private readonly Dictionary<string, List<string>> _values = new(StringComparer.OrdinalIgnoreCase);

        public List<string> Positional { get; } = [];

        public static Options Parse(IEnumerable<string> args)
        {
            var options = new Options();
            string? pending = null;
            foreach (var arg in args)
            {
                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    if (pending is not null)
                        options.Add(pending, "true");
                    pending = arg[2..];
                }
                else if (pending is not null)
                {
                    options.Add(pending, arg);
                    pending = null;
                }
                else
                {
                    options.Positional.Add(arg);
                }
            }
            if (pending is not null)
                options.Add(pending, "true");
            return options;
        }

        public string Required(string name) =>
            _values.TryGetValue(name, out var v) ? v[^1] : throw new ArgumentException($"--{name} is required");

        public IEnumerable<string> All(string name) => _values.TryGetValue(name, out var v) ? v : [];

        public bool Flag(string name) => _values.ContainsKey(name);

        private void Add(string name, string value)
        {
            if (!_values.TryGetValue(name, out var list))
                _values[name] = list = [];
            list.Add(value);
        }
    }
}
