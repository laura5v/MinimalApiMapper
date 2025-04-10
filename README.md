# MinimalApiMapper

[![NuGet Version](https://img.shields.io/nuget/v/MinimalApiMapper.SourceGenerator.svg)](https://www.nuget.org/packages/MinimalApiMapper.SourceGenerator/)
[![NuGet Version](https://img.shields.io/nuget/v/MinimalApiMapper.Abstractions.svg)](https://www.nuget.org/packages/MinimalApiMapper.Abstractions/)

**Tired of choosing between the clean organization of MVC Controllers and the blazing-fast, Native AOT power of Minimal APIs? Now you don't have to!**

MinimalApiMapper lets you build highly organized, dependency-injected API endpoints using familiar scoped classes, while achieving full Native AOT compatibility and leveraging ASP.NET Core's performance optimizations, like compile-time Request Delegate Generators and System.Text.Json Source Generators.

## The Problem

ASP.NET Core Minimal APIs offer better performance for APIs without views, and Native AOT support, resulting in smaller, faster applications. However, organizing endpoints directly in `Program.cs` using `app.MapGet(...)`, `app.MapPost(...)`, etc., can become unwieldy for larger APIs.

Traditional MVC Controllers offer better organization but rely heavily on runtime reflection, which hinders effective Native AOT trimming and performance optimization provided by the newer interceptor-based [Request Delegate Generator (RDG)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/aot/request-delegate-generator/rdg).

## The Solution

MinimalApiMapper uses a Roslyn source generator to bridge this gap:

1.  **Organized API Groups:** Define API endpoints within simple C# classes using `[MapGroup]` and methods with `[MapGet]`, `[MapPost]`, etc. Inject dependencies via constructors just like you're used to – **scoped DI is automatically handled!**
2.  **Compile-Time Magic:** The incremental source generator analyzes your API groups at build time. Generating the raw `app.MapX(...)` calls required by Minimal APIs.
3.  **True AOT Speed:** By integrating with the build process, this code becomes visible to ASP.NET's Request Delegate Generator, which transforms the generated `MapX` calls into optimized, reflection-free delegates using interceptors, unlocking the full performance potential of Native AOT.
4.  **Seamless Integration:** Your existing attributes (`[Authorize]`, `[FromBody]`, `[FromQuery]`, etc.) are automatically transferred to the generated code, ensuring perfect compatibility equivalent to manual Minimum API registrations.
5.  **Optional JSON Support:** It can automatically generate a `JsonSerializerContext` for types used in your API endpoints to simplify Native AOT configuration for `System.Text.Json`. Just call `builder.Services.AddApiGroupSerializers()`.

**MinimalApiMapper isn't a new framework, it's just an organized way of registering ASP.NET Minimal APIs. As such any existing features, extensions, and security mechanisms of ASP.NET Core should work as expected.**

## Installation

Add the `MinimalApiMapper.SourceGenerator` NuGet package to your project. This will include the `MinimalApiMapper.Abstractions` dependency as well. Only this abstractions assembly will be included in your build output.

```bash
dotnet add package MinimalApiMapper.SourceGenerator
```

## Configuration: Generated Code Output

To ensure the ASP.NET Core Request Delegate Generator works correctly for Native AOT, MinimalApiMapper needs to copy its generated files into your project's source tree during the build. This is a [current limitation of Roslyn source generators](https://github.com/dotnet/roslyn/discussions/48358), but it may change in the future.

You can customize the output path in your `.csproj` by adding the `MinimalApiMapper_GeneratedOutput` property:

```xml
<PropertyGroup>
  <!-- Supports any relative or absolute path -->
  <MinimalApiMapper_GeneratedOutput>Generated/MinimalApiMapper</MinimalApiMapper_GeneratedOutput>
</PropertyGroup>
```

**Tip:** You can add this folder to your `.gitignore` file if you prefer not to check in the generated files.

## Quick Start Example

1.  **Define your API Group:**

    ```csharp
    // ApiGroups/UserApiGroup.cs
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.Extensions.Logging;
    using MinimalApiMapper.Abstractions;
    using System.Security.Claims;

    namespace YourApp.ApiGroups;

    [MapGroup("api/users")] // Base route prefix for this group
    public class UserApiGroup(ILogger<UserApiGroup> logger, IUserService userService) // Scoped Constructor DI
    {
        // Maps to GET /api/users/hello?name=world
        [MapGet("hello")]
        public Ok<string> GetHello(string? name = "world")
        {
            logger.LogInformation("Saying hello to {Name}", name);
            return TypedResults.Ok($"Hello, {name}!");
        }

        // Maps to GET /api/users/me
        [MapGet("me")]
        [Authorize] // Standard attributes work!
        public Results<Ok<string>, UnauthorizedHttpResult> GetCurrentUser(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return TypedResults.Unauthorized();
            }
            logger.LogInformation("Getting current user: {UserId}", userId);
            // Use injected service
            var username = userService.GetUsername(userId);
            return TypedResults.Ok($"Authenticated as: {username} (ID: {userId})");
        }

        // Maps to POST /api/users
        [MapPost("")]
        public async Task<Results<Created<UserDto>, BadRequest<string>>> CreateUser(
            [FromBody] CreateUserRequest request
        ) // Parameter attributes work!
        {
            logger.LogInformation("Attempting to create user {Username}", request.Username);
            var result = await userService.CreateUserAsync(request);
            if (!result.Success)
            {
                return TypedResults.BadRequest(result.ErrorMessage);
            }
            return TypedResults.Created($"/api/users/{result.User.Id}", result.User);
        }
    }

    // Define your DTOs and Services elsewhere
    public record CreateUserRequest(string Username, string Password);
    public record UserDto(string Id, string Username);
    public interface IUserService { /* ... methods ... */ string GetUsername(string id); Task<(bool Success, UserDto User, string ErrorMessage)> CreateUserAsync(CreateUserRequest request); }
    public class UserService : IUserService { /* ... implementation ... */ }
    ```

2.  **Setup in `Program.cs`:**

    ```csharp
    using MinimalApiMapper.Generated; // Generated extensions namespace
    using YourApp.ApiGroups;
    using YourApp.Services;

    var builder = WebApplication.CreateSlimBuilder(args);

    // Configure your services
    builder.Services.AddScoped<IUserService, UserService>();

    // 1. (Optional) Add generated JSON serializer context for request types
    builder.Services.AddApiGroupSerializers();
    // 2. Register API Groups for Scoped DI
    builder.Services.AddApiGroups();

    var app = builder.Build();
    
    // 3. Map the generated endpoints
    app.MapApiGroups();

    app.Run();
    ```

## Attribute Reference

Use these attributes from `MinimalApiMapper.Abstractions`, they correspond to the `app.MapX(...)` methods:

*   **`[MapGroup(string prefix)]`**: Apply to a class to define it as an API group with a common route prefix.
*   **`[MapGet(string? template)]`**: Apply to a method to map it to HTTP GET. The template is relative to the group prefix.
*   **`[MapPost(string? template)]`**: Apply to a method to map it to HTTP POST.
*   **`[MapPut(string? template)]`**: Apply to a method to map it to HTTP PUT.
*   **`[MapDelete(string? template)]`**: Apply to a method to map it to HTTP DELETE.
*   **`[MapPatch(string? template)]`**: Apply to a method to map it to HTTP PATCH.
*   **`[MapMethods(string? template, params string[] httpMethods)]`**: Apply to a method to map it to multiple specific HTTP methods (e.g. `"GET"`, `"HEAD"`).

## Contributing

Contributions are welcome! Please refer to `CONTRIBUTING.md` for guidelines.

## License

This project is licensed under the MIT License - see the `LICENSE` file for details.
