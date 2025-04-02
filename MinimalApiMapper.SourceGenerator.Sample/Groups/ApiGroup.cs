using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiMapper.Abstractions;

namespace MinimalApiMapper.SourceGenerator.Sample.Groups;

[MapGroup("api")]
public class ApiGroup(ILogger<ApiGroup> logger)
{
    [MapGet("hello")]
    public Ok<string> GetHello(string? name = null)
    {
        logger.LogInformation("GetHello: {Name}", name);
        
        return TypedResults.Ok($"Hello {name}!");
    }
    
    [Authorize]
    [MapGet("auth")]
    public Ok<ModelResponse> GetAuth(ClaimsPrincipal user, string? data = null)
    {
        return TypedResults.Ok(new ModelResponse
        {
            User = user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
            Message = $"Authenticated as: {user.Identity?.Name}",
            Data = data,
        });
    }
}
