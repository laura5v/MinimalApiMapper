using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiMapper.Abstractions;

namespace MinimalApiMapper.SourceGenerator.Sample;

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
    [MapGet("auth/{id}")]
    public Ok<string> GetAuth(ClaimsPrincipal user, string id)
    {
        logger.LogInformation("User: {Name}", user.Identity?.Name);
        
        return TypedResults.Ok($"Auth {id}!");
    }
}
