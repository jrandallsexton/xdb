using System;
using System.IO;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// The original service ran on a Windows host in US Eastern time, and its wire carries local dates (serializer
/// offsets, feed dates, date texts). On hosts that read IANA zone data (Linux containers) the process's local zone
/// is set to Eastern.tzif, Windows' "Eastern Standard Time" rules written as a zone file
/// (tools/LegacyTimeZone/generate-eastern-tzif.py): IANA's own America/New_York carries real history that Windows
/// does not (local mean time before 1883, older rules), so the same dates would be written differently. A Windows
/// host uses its own zone and must be set to Eastern Standard Time.
/// </summary>
public static class LegacyTimeZone
{
    public const string FileName = "Eastern.tzif";

    private static bool _applied;

    /// <summary>
    /// Points TZ at the bundled zone file and drops any local zone already cached. Called first by the API's Main and
    /// by the static constructors of the types that write legacy dates, so tests get it too. Idempotent.
    /// </summary>
    public static void Apply()
    {
        if (_applied || OperatingSystem.IsWindows())
            return;
        _applied = true;
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("The legacy time zone file is missing from the application directory.", path);
        Environment.SetEnvironmentVariable("TZ", path);
        TimeZoneInfo.ClearCachedData();
    }
}
