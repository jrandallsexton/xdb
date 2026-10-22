namespace Xdb.Compare.Comparison;

/// <param name="Ignore">Paths whose values may differ (reported as ignored).</param>
/// <param name="Masks">Body masks applied before comparison.</param>
/// <param name="Unordered">
/// Paths of arrays compared as multisets: for results whose order the original itself does not guarantee
/// (e.g. a query without ORDER BY). An order-only difference is reported as ignored.
/// </param>
public sealed record ComparisonOptions(IgnoreRules Ignore, BodyMasks Masks, IgnoreRules Unordered)
{
    public static ComparisonOptions Strict { get; } = new(IgnoreRules.None, BodyMasks.None, IgnoreRules.None);
}
