using System;
using System.Globalization;

namespace Xdb.Api.Application.GeneratedObjects;

/// <summary>
/// The string order the original got from SortedDictionary&lt;string, _&gt; under the en-US culture (case-sensitive,
/// culture-aware). The original ran on Windows NLS; .NET 10 uses ICU. They agree on every display name in the
/// working database. Known difference: NLS "word sort" nearly ignores hyphens and apostrophes, ICU does not, so a
/// pair like "ab-c" / "ab'x" can order differently (recorded as a deviation).
/// </summary>
public static class LegacyCollation
{
    public static readonly StringComparer EnUs = StringComparer.Create(CultureInfo.GetCultureInfo("en-US"), ignoreCase: false);
}
