using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiMapper.Abstractions;

namespace MinimalApiMapper.SourceGenerator.Sample.Groups;

[MapGroup("home")]
public class HomeGroup(ILogger<HomeGroup> logger)
{
    [Authorize]
    [MapGet("auth")]
    public Ok<ModelResponse> GetAuth(ClaimsPrincipal user, string? data = null)
    {
        logger.LogInformation("GetAuth: {User}", user);
        
        return TypedResults.Ok(new ModelResponse
        {
            User = user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
            Message = $"Authenticated as: {user.Identity?.Name}",
            Data = data,
        });
    }
    
    [MapGet("hello")]
    public Ok<string> GetHello(string? name = null)
    {
        logger.LogInformation("GetHello: {Name}", name);
        
        return TypedResults.Ok($"Hello {name}!");
    }
    

}
