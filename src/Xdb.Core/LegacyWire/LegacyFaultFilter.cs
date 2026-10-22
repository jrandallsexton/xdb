using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// Reproduces the original host's handling of an exception escaping an operation: HTTP 500 with the generic
/// "Detail" body. Unlike the original (which wrote only to the Windows event log), the exception is logged.
/// </summary>
public sealed class LegacyFaultFilter(ILogger<LegacyFaultFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        logger.LogError(context.Exception, "Legacy operation {Operation} failed", context.ActionDescriptor.DisplayName);
        context.Result = LegacyWire.Fault();
        context.ExceptionHandled = true;
    }
}
