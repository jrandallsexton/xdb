using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Xdb.Compare.Comparison;

/// <summary>
/// Regular expressions applied to both bodies before comparison: the text captured by a group named "mask" is
/// replaced with "*". For values that legitimately differ between runs inside a larger value (e.g. an elapsed time
/// inside a message), where a path-based ignore rule would hide the whole value.
/// </summary>
public sealed class BodyMasks
{
    private readonly IReadOnlyList<Regex> _patterns;

    public BodyMasks(IEnumerable<string> patterns) =>
        _patterns = patterns.Select(p => new Regex(p, RegexOptions.CultureInvariant)).ToList();

    public static BodyMasks None { get; } = new([]);

    public bool IsEmpty => _patterns.Count == 0;

    public byte[] Apply(byte[] body)
    {
        if (IsEmpty)
            return body;

        var text = Encoding.UTF8.GetString(body);
        foreach (var pattern in _patterns)
            text = pattern.Replace(text, match => Mask(match));
        return Encoding.UTF8.GetBytes(text);
    }

    private static string Mask(Match match)
    {
        var group = match.Groups["mask"];
        if (!group.Success)
            return match.Value;
        var start = group.Index - match.Index;
        return match.Value[..start] + "*" + match.Value[(start + group.Length)..];
    }
}
