using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// Reproduces how the original host converted typed query parameters. A value that cannot be converted, an empty
/// value ("x="), a parameter given twice, or an absent enum parameter all produce the host's 1047-byte HTML
/// request-error page (HTTP 500); absent Guid/int/bool parameters bind their defaults. Enums accept names in any
/// case and any integer, defined or not, which ASP.NET's own binder rejects, so they are converted here.
/// String parameters never fail at binding.
/// </summary>
public sealed class LegacyBindingFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var query = context.HttpContext.Request.Query;
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            var type = parameter.ParameterType;
            var source = parameter.BindingInfo?.BindingSource;
            if (source is not null && source != BindingSource.Query && source != BindingSource.ModelBinding)
                continue;
            if (type == typeof(string))
            {
                // The original passed an empty value as "" (only an absent one is null); ASP.NET would bind null.
                var text = query[parameter.BindingInfo?.BinderModelName ?? parameter.Name];
                if (text.Count == 1 && text[0] == string.Empty)
                    context.ActionArguments[parameter.Name] = string.Empty;
                continue;
            }
            if (!IsSimple(type))
                continue;

            var name = parameter.BindingInfo?.BinderModelName ?? parameter.Name;
            var values = query[name];
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (values.Count > 1 || (values.Count == 1 && string.IsNullOrEmpty(values[0])))
                return Reject(context);

            if (underlying.IsEnum)
            {
                if (values.Count == 0 || !Enum.TryParse(underlying, values[0], ignoreCase: true, out var parsed))
                    return Reject(context);
                context.ActionArguments[parameter.Name] = parsed;
                context.ModelState.Remove(name);
                continue;
            }

            if (context.ModelState.TryGetValue(name, out var entry) && entry.ValidationState == ModelValidationState.Invalid)
                return Reject(context);
        }

        return next();
    }

    private static Task Reject(ActionExecutingContext context)
    {
        context.Result = LegacyWire.RequestError();
        return Task.CompletedTask;
    }

    private static bool IsSimple(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(Guid) || underlying == typeof(decimal)
            || underlying == typeof(DateTime) || new[] { typeof(short), typeof(long) }.Contains(underlying);
    }
}
