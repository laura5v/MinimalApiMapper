
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace MinimalApiMapper.SourceGenerator.Tests;

[TestClass]
public class ApiMapperGeneratorTests : VerifyBase
{

    private static GeneratorDriver BuildDriver(
        [StringSyntax("CSharp"), LanguageInjection("csharp")] string source
    )
    {
        var sourceText = SourceText.From(source);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);

        var compilation = CSharpCompilation.Create("Test.dll", [syntaxTree]);
        var generator = new ApiMapperGenerator { WriteToSource = false };

        var driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
    }

    [TestMethod]
    public Task SimpleApiGroup_GeneratesCorrectExtensions() // Must return Task
    {
        return TestHelper.VerifyGenerator(
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Logging;
            using System.Security.Claims;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.AspNetCore.Authorization;
            using MinimalApiMapper.Abstractions;
            using System.Text.Json.Serialization;

            namespace MyTestApi.Groups;

            // Simple DTO for testing JSON serialization
            public record TestDto(int Id, string Name);

            [MapGroup("api/test")]
            public class TestApiGroup
            {
                private readonly ILogger<TestApiGroup> _logger;

                // Constructor Injection
                public TestApiGroup(ILogger<TestApiGroup> logger)
                {
                    _logger = logger;
                }

                [MapGet("hello")]
                public IResult GetHello([FromQuery] string name = "World")
                {
                    _logger.LogInformation("Test API Hello to {Name}", name);
                    return Results.Ok($"Hello {name} from TestApiGroup");
                }

                [MapPost("dto")]
                [AllowAnonymous] // Example attribute passthrough
                public Results<Created<TestDto>, BadRequest> PostDto([FromBody] TestDto dto)
                {
                    if (dto.Id <= 0) 
                        return TypedResults.BadRequest();
                    
                    _logger.LogInformation("Received DTO: {DtoId} - {DtoName}", dto.Id, dto.Name);

                    return TypedResults.Created($"/api/test/dto/{dto.Id}", dto);
                }

                [MapGet("voidreturn")]
                public void GetVoid() 
                { 
                }

                [MapDelete("asyncdelete/{id}")]
                public async Task<Results<NoContent, NotFound>> DeleteAsync(int id)
                {
                    await Task.Delay(10); // Simulate async work
                    return id > 0 ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            }
            """
        );
    }

    [TestMethod]
    public Task NoGroups_GeneratesNothing()
    {
        return TestHelper.VerifyGenerator(
            """
            namespace MyTestApi.Groups;
            public class NotAnApiGroup {}
            """
        );
    }
}
