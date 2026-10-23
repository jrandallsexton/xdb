using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace Xdb.Core.LegacyWire;

/// <summary>The host closed the connection without a response (status 0 to the client): the request is aborted.</summary>
public sealed class LegacyConnectionReset : IActionResult
{
    public Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Abort();
        return Task.CompletedTask;
    }
}
