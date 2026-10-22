using System.Collections.Generic;
using System.Linq;

namespace Xdb.Compare.Comparison;

public enum ComparisonGrade
{
    /// <summary>Status, content type and body bytes are identical.</summary>
    Identical,

    /// <summary>Only configured (ignored) values differ.</summary>
    Equivalent,

    Different
}

public sealed record ComparisonResult(IReadOnlyList<Difference> Differences)
{
    public ComparisonGrade Grade =>
        Differences.Count == 0 ? ComparisonGrade.Identical
        : Differences.All(d => d.Ignored) ? ComparisonGrade.Equivalent
        : ComparisonGrade.Different;
}
