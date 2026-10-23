using System;
using System.Linq;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// Reproduces the original host's handling of an exception escaping an operation: HTTP 500 with the generic
/// "Detail" body; an operation with XML responses (<see cref="LegacyXmlOperationAttribute"/>) answered the Request
/// Error page instead. Unlike the original (which wrote only to the Windows event log), the exception is logged.
/// </summary>
public sealed class LegacyFaultFilter(ILogger<LegacyFaultFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        logger.LogError(context.Exception, "Legacy operation {Operation} failed", context.ActionDescriptor.DisplayName);
        context.Result = context.ActionDescriptor.EndpointMetadata.OfType<LegacyXmlOperationAttribute>().Any() ? LegacyWire.RequestError() : LegacyWire.Fault();
        context.ExceptionHandled = true;
    }
}

/// <summary>An operation whose original response format was XML (RSS feeds): any exception was the Request Error page.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LegacyXmlOperationAttribute : Attribute;
