namespace Xdb.Compare.Comparison;

/// <param name="Ignore">Paths whose values may differ (reported as ignored).</param>
/// <param name="Masks">Body masks applied before comparison.</param>
/// <param name="Unordered">
/// Paths of arrays compared as multisets: for results whose order the original itself does not guarantee
/// (e.g. a query without ORDER BY). An order-only difference is reported as ignored.
/// </param>
/// <param name="UnorderedXmlItems">
/// For a non-JSON body: compare the RSS &lt;item&gt; elements as a multiset (the text before the first item and after
/// the last one exactly), for feeds whose item order the original does not guarantee.
/// </param>
/// <param name="Xlsx">Compare workbook bodies as packages (XlsxComparer) and the Content-Disposition with its file-name date masked.</param>
public sealed record ComparisonOptions(IgnoreRules Ignore, BodyMasks Masks, IgnoreRules Unordered, bool UnorderedXmlItems = false, bool Xlsx = false)
{
    public static ComparisonOptions Strict { get; } = new(IgnoreRules.None, BodyMasks.None, IgnoreRules.None);
}
