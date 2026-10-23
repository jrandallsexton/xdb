using System;
using System.Globalization;
using System.Text.Json;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Roles.Legacy;

namespace Xdb.Api.Application.Common.Legacy;

/// <summary>
/// The Migrate operations' KeyValuePair&lt;Guid, EApplicationInstance&gt; body as the original serializer bound it
/// (spec M6-M 5.0, FX K): lowercase <c>key</c> and <c>value</c> both required and non-null, the key a Guid, the value
/// any integer (quoted or not). An empty or literal null body is the default pair (empty id, LOCAL).
/// </summary>
public static class MigrateBody
{
    public static (Guid Key, int Target) Read(JsonElement? root)
    {
        if (root is null || root.Value.ValueKind == JsonValueKind.Null)
            return (Guid.Empty, 0);
        var r = root.Value;
        if (r.ValueKind != JsonValueKind.Object || !r.TryGetProperty("key", out var key) || !r.TryGetProperty("value", out var value))
            throw new LegacyBodyException("Missing a required member.");
        if (key.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Null)
            throw new LegacyBodyException("A value type cannot be null.");
        return (RoleBodies.Guid(key), LegacyJsonBody.Int(value));
    }

    /// <summary>Whether the target is the instance this service runs as (the only database the port can reach).</summary>
    public static bool IsOwnInstance(int target, string applicationInstance) =>
        target.ToString(CultureInfo.InvariantCulture) == applicationInstance;
}
