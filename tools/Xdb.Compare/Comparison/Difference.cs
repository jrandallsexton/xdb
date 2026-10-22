namespace Xdb.Compare.Comparison;

public enum DifferenceKind
{
    Status,
    ContentType,
    ErrorIdHeader,
    Value,
    ValueKind,
    MissingProperty,
    ExtraProperty,
    PropertyOrder,
    ArrayLength,
    ArrayOrder,
    NonJsonBody,
    WhitespaceOnly
}

/// <param name="Path">JSON path ("$.rows[0].cell[3]") or a pseudo-path for non-body parts ("$status").</param>
/// <param name="Ignored">True when the path matches a configured ignore rule; reported but not failing.</param>
public sealed record Difference(string Path, DifferenceKind Kind, string? Legacy, string? Port, bool Ignored = false);
