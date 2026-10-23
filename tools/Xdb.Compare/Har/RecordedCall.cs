namespace Xdb.Compare.Har;

/// <summary>One call to the service, as recorded in a capture.</summary>
/// <param name="Operation">Operation name, e.g. "Report_GetData".</param>
/// <param name="RelativeUrl">Everything after "Service.svc/", including the query string.</param>
public sealed record RecordedCall(
    int Index,
    string Operation,
    string Method,
    string RelativeUrl,
    string? RequestBody,
    string? RequestContentType,
    int RecordedStatus,
    string? RecordedContentType,
    string? RecordedBody,
    byte[]? RequestBodyBytes = null);
