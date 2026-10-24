using System;
using System.Globalization;

using Xdb.Api.Application.Common.Metadata;

namespace Xdb.Api.Application.Assets.Commands.CreateAsset;

/// <summary>
/// The canonical text new code stores for a value of each data type. Legacy rows hold en-US forms (m/d/yyyy dates,
/// local times); new rows hold ISO dates, UTC date-times and invariant numbers. Reference types (PickList, Asset,
/// User) parse to an id here; whether the id exists is the caller's check.
/// </summary>
public static class PropertyValueText
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public static bool IsSupported(DataType type) =>
        type is DataType.String or DataType.Memo or DataType.Int or DataType.Float or DataType.Date or DataType.DateTime
            or DataType.PickList or DataType.Asset or DataType.User;

    /// <returns>The stored text, or null with <paramref name="error"/> set when the value is not of the type.</returns>
    public static string? Normalize(DataType type, string value, out string? error)
    {
        error = null;
        switch (type)
        {
            case DataType.String:
            case DataType.Memo:
                return value;

            case DataType.Int:
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    return number.ToString(CultureInfo.InvariantCulture);
                break;

            case DataType.Float:
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var real) && double.IsFinite(real))
                    return real.ToString("R", CultureInfo.InvariantCulture);
                break;

            case DataType.Date:
                if (DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    return date.ToString(DateFormat, CultureInfo.InvariantCulture);
                break;

            case DataType.DateTime:
                if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var moment))
                    return moment.UtcDateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
                break;

            case DataType.PickList:
            case DataType.Asset:
            case DataType.User:
                // 36 characters: the gen-table decoders cast exactly that.
                if (Guid.TryParse(value, out var id))
                    return id.ToString("D");
                break;

            default:
                error = $"Values of data type {type} are not supported yet.";
                return null;
        }

        error = $"'{value}' is not a valid {type} value.";
        return null;
    }
}
