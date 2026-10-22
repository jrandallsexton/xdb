namespace Xdb.Api.Application.GeneratedObjects;

/// <summary>Configuration for the generated objects (section "GeneratedObjects").</summary>
public sealed class GeneratedObjectsOptions
{
    public const string SectionName = "GeneratedObjects";

    /// <summary>
    /// Base URL for viewing images. The original baked it into every Image column of the gen tables as a string
    /// literal (an environment-specific host); regenerated tables bake in this value instead.
    /// </summary>
    public string ViewImageUrl { get; set; } = string.Empty;
}
