using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Xdb.Api.Infrastructure.Data.Scripts;

/// <summary>Reads SQL scripts embedded from Infrastructure/Data/Scripts (see the API csproj).</summary>
public static class SqlScripts
{
    private static readonly Assembly Assembly = typeof(SqlScripts).Assembly;

    /// <param name="relativePath">Path under Scripts, e.g. "Baseline/0001_tables.sql".</param>
    public static string Read(string relativePath)
    {
        var suffix = ".Infrastructure.Data.Scripts." + relativePath.Replace('/', '.').Replace('\\', '.');
        var name = Assembly.GetManifestResourceNames().SingleOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded SQL script not found: {relativePath}");

        using var stream = Assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
