using System;

using Microsoft.AspNetCore.Mvc;

using Xdb.Core.Common;

namespace Xdb.Core.LegacyWire;

public static class LegacyResultExtensions
{
    /// <summary>
    /// Maps a handler result onto the legacy wire. The original had no status mapping: an operation either returned
    /// its value or failed with the host's generic 500, so every failure becomes that fault.
    /// </summary>
    public static IActionResult ToLegacyResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : LegacyWire.Fault();
}
