using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace MinimalApiMapper.Abstractions;

/// <summary>
/// Specifies that a class represents a group of API endpoints with a common route prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MapGroupAttribute : Attribute
{
    /// <summary>
    /// Gets the route prefix for the group.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapGroupAttribute"/> class.
    /// </summary>
    /// <param name="prefix">The route prefix for the group.</param>
    public MapGroupAttribute([StringSyntax("Route"), RouteTemplate] string prefix)
    {
        // Ensure prefix doesn't start or end with '/' to avoid double slashes when combining
        Prefix = prefix?.Trim('/') ?? string.Empty;
    }
}

/// <summary>
/// Base attribute for mapping HTTP methods to endpoint handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public abstract class MapMethodAttribute : Attribute
{
    /// <summary>
    /// Gets the route template for the endpoint.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapMethodAttribute"/> class.
    /// </summary>
    /// <param name="template">The route template for the endpoint.</param>
    protected MapMethodAttribute([StringSyntax("Route"), RouteTemplate] string? template)
    {
        // Allow empty template for root of the group
        Template = template ?? string.Empty;
    }
}

/// <summary>
/// Maps an endpoint handler to HTTP GET requests.
/// </summary>
public sealed class MapGetAttribute : MapMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapGetAttribute"/> class.
    /// </summary>
    /// <param name="template">The route template for the endpoint.</param>
    public MapGetAttribute([StringSyntax("Route"), RouteTemplate] string? template = null)
        : base(template) { }
}

/// <summary>
/// Maps an endpoint handler to HTTP POST requests.
/// </summary>
public sealed class MapPostAttribute : MapMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapPostAttribute"/> class.
    /// </summary>
    /// <param name="template">The route template for the endpoint.</param>
    public MapPostAttribute([StringSyntax("Route"), RouteTemplate] string? template = null)
        : base(template) { }
}

/// <summary>
/// Maps an endpoint handler to HTTP PUT requests.
/// </summary>
public sealed class MapPutAttribute : MapMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapPutAttribute"/> class.
    /// </summary>
    /// <param name="template">The route template for the endpoint.</param>
    public MapPutAttribute([StringSyntax("Route"), RouteTemplate] string? template = null)
        : base(template) { }
}

/// <summary>
/// Maps an endpoint handler to HTTP DELETE requests.
/// </summary>
public sealed class MapDeleteAttribute : MapMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapDeleteAttribute"/> class.
    /// </summary>
    /// <param name="template">The route template for the endpoint.</param>
    public MapDeleteAttribute([StringSyntax("Route"), RouteTemplate] string? template = null)
        : base(template) { }
}

/// <summary>
/// Maps an endpoint handler to HTTP PATCH requests.
/// </summary>
public sealed class MapPatchAttribute : MapMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapPatchAttribute"/> class.
    /// </summary>
    /// <param name="template">The route template for the endpoint.</param>
    public MapPatchAttribute([StringSyntax("Route"), RouteTemplate] string? template = null)
        : base(template) { }
}

/// <summary>
/// Maps an endpoint handler to one or more specified HTTP methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class MapMethodsAttribute : Attribute // Does not inherit MapMethodAttribute as it takes methods explicitly
{
    /// <summary>
    /// Gets the route template for the endpoint.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Gets the collection of HTTP methods handled by the endpoint.
    /// </summary>
    public IEnumerable<string> HttpMethods { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapMethodsAttribute"/> class.
    /// </summary>
    /// <param name="template">The route template for the endpoint.</param>
    /// <param name="httpMethods">A collection of HTTP methods (e.g., "GET", "POST").</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpMethods"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="httpMethods"/> is empty or contains null/empty strings.</exception>
    public MapMethodsAttribute(
        [StringSyntax("Route"), RouteTemplate] string? template,
        params string[] httpMethods
    )
    {
        if (httpMethods == null)
            throw new ArgumentNullException(nameof(httpMethods));
        if (!httpMethods.Any() || httpMethods.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException(
                "HTTP methods collection cannot be empty or contain null/whitespace methods.",
                nameof(httpMethods)
            );

        Template = template ?? string.Empty;
        HttpMethods = httpMethods.Select(m => m.ToUpperInvariant()).ToList(); // Store uppercase for consistency
    }
}
