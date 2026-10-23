using System;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// An operation that reads its request body itself, whatever the Content-Type (the original's Stream parameters).
/// Removes the form value providers, which would otherwise consume a multipart or url-encoded body during model
/// binding before the action reads it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LegacyRawBodyAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        context.ValueProviderFactories.RemoveType<FormValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<FormFileValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
