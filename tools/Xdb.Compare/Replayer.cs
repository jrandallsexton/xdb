using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Compare.Comparison;
using Xdb.Compare.Har;

namespace Xdb.Compare;

/// <summary>Sends a recorded call to a service base URL and captures the response as it arrived on the wire.</summary>
public sealed class Replayer(TimeSpan? timeout = null) : IDisposable
{
    private readonly HttpClient _http = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        UseCookies = false
    })
    {
        // Some operations (RGraph on a well-connected asset) take ~10 minutes on both hosts.
        Timeout = timeout ?? TimeSpan.FromMinutes(5)
    };

    public async Task<CapturedResponse> SendAsync(string baseUrl, RecordedCall call, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(new HttpMethod(call.Method), baseUrl.TrimEnd('/') + "/" + call.RelativeUrl);
        if (call.RequestBodyBytes is not null || call.RequestBody is not null)
        {
            request.Content = new ByteArrayContent(call.RequestBodyBytes ?? Encoding.UTF8.GetBytes(call.RequestBody!));
            if (call.RequestContentType is not null)
                request.Content.Headers.TryAddWithoutValidation("Content-Type", call.RequestContentType);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // The host closed the connection without a response (the original resets it on some wrapped bodies):
            // recorded as status 0, so a reset on both sides compares equal and a reset on one side differs.
            return new CapturedResponse(0, null, []);
        }
        using var disposable = response;
        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.TryGetValues("Content-Type", out var values) ? string.Join(",", values) : null;
        var errorId = response.Headers.TryGetValues("ErrorId", out var ids) ? ids.FirstOrDefault() : null;
        var disposition = response.Content.Headers.TryGetValues("Content-Disposition", out var dispositions) ? string.Join(",", dispositions) : null;
        return new CapturedResponse((int)response.StatusCode, contentType, body, errorId, disposition);
    }

    public void Dispose() => _http.Dispose();
}
