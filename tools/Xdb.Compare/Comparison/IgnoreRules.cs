using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Xdb.Compare.Comparison;

/// <summary>
/// Path patterns: "$" root, ".name" property, "[n]" index, "[*]" any index, ".*" any property,
/// "..name" that property at any depth. Example: "$.rows[*].cell[3]".
/// </summary>
public sealed class IgnoreRules
{
    private readonly IReadOnlyList<Regex> _patterns;

    public IgnoreRules(IEnumerable<string> patterns) =>
        _patterns = patterns.Select(ToRegex).ToList();

    public static IgnoreRules None { get; } = new([]);

    public bool Matches(string path) => _patterns.Any(p => p.IsMatch(path));

    private static Regex ToRegex(string pattern)
    {
        var regex = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            if (Starts(pattern, i, ".."))
            {
                regex.Append(@"(?:\.[^.\[]+|\[\d+\])*\.");
                i += 1;
            }
            else if (Starts(pattern, i, "[*]"))
            {
                regex.Append(@"\[\d+\]");
                i += 2;
            }
            else if (Starts(pattern, i, ".*"))
            {
                regex.Append(@"\.[^.\[]+");
                i += 1;
            }
            else
            {
                regex.Append(Regex.Escape(pattern[i].ToString()));
            }
        }
        return new Regex(regex.Append('$').ToString(), RegexOptions.CultureInvariant);
    }

    private static bool Starts(string pattern, int index, string token) =>
        string.CompareOrdinal(pattern, index, token, 0, token.Length) == 0;
}
