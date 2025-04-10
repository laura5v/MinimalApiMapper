using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MinimalApiMapper.Abstractions;

namespace MinimalApiMapper.SourceGenerator.Tests;

public record TestOptions
{
    public string AssemblyName { get; init; } = "MinimalApiMapper.SourceGenerator.Tests";
    public LanguageVersion LanguageVersion { get; init; } = LanguageVersion.CSharp10;
    public NullableContextOptions NullableOption { get; init; } = NullableContextOptions.Enable;

    public static TestOptions Default { get; } = new();
}

public static class TestHelper
{
    private static readonly GeneratorDriverOptions IncrementalTrackingDriverOptions =
        new(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);

    public static Task<VerifyResult> VerifyGenerator(
        [StringSyntax("CSharp"), LanguageInjection("csharp")] string source,
        params object?[] args
    )
    {
        var driver = Generate(source);
        var verify = Verify(driver);

        if (args.Length != 0)
        {
            verify.UseParameters(args);
        }

        return verify.ToTask();
    }

    public static CSharpCompilation BuildCompilation(params SyntaxTree[] syntaxTrees) =>
        BuildCompilation("Tests", NullableContextOptions.Enable, true, syntaxTrees);

    public static GeneratorDriver GenerateTracked(Compilation compilation)
    {
        var generator = new ApiMapperGenerator();

        var driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: IncrementalTrackingDriverOptions
        );
        return driver.RunGenerators(compilation);
    }

    public static CSharpCompilation BuildCompilation(
        [StringSyntax("CSharp")] string source,
        TestOptions? options = null
    )
    {
        options ??= TestOptions.Default;
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(options.LanguageVersion)
        );
        return BuildCompilation(options.AssemblyName, options.NullableOption, true, syntaxTree);
    }

    private static GeneratorDriver Generate([StringSyntax("CSharp")] string source)
    {
        var compilation = BuildCompilation(source);

        var generator = new ApiMapperGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
    }

    private static CSharpCompilation BuildCompilation(
        string name,
        NullableContextOptions nullableOption,
        bool addReferences,
        params SyntaxTree[] syntaxTrees
    )
    {
        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: nullableOption
        );
        var compilation = CSharpCompilation.Create(name, syntaxTrees, options: compilationOptions);

        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location));
        compilation = compilation.AddReferences(references);

        if (addReferences)
        {
            compilation = compilation.AddReferences(
                MetadataReference.CreateFromFile(typeof(MapGetAttribute).Assembly.Location)
            );
        }

        return compilation;
    }

    private static IEnumerable<MethodDeclarationSyntax> ExtractAllMethods(SyntaxNode? root)
    {
        if (root == null)
            yield break;

        foreach (var node in root.ChildNodes())
        {
            // a namespace can contain classes
            if (node is NamespaceDeclarationSyntax)
            {
                foreach (var method in ExtractAllMethods(node))
                {
                    yield return method;
                }
            }

            // a class can contain methods or other classes
            if (node is not ClassDeclarationSyntax classNode)
                continue;

            foreach (var method in classNode.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                yield return method;
            }

            foreach (var method in ExtractAllMethods(node))
            {
                yield return method;
            }
        }
    }
}
