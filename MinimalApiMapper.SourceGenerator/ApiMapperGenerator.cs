// MinimalApiMapper.SourceGenerator/ApiMapperGenerator.cs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MinimalApiMapper.Abstractions; // Make sure this using is present

namespace MinimalApiMapper.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class ApiMapperGenerator : IIncrementalGenerator
{
    // Define the fully qualified names of our attributes
    private const string MapGroupAttributeName = "MinimalApiMapper.Abstractions.MapGroupAttribute";
    private const string MapMethodAttributeBaseName =
        "MinimalApiMapper.Abstractions.MapMethodAttribute"; // Base class for GET, POST etc.
    private const string MapMethodsAttributeName =
        "MinimalApiMapper.Abstractions.MapMethodsAttribute"; // Specific attribute for MapMethods(...)

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // --- Step 1: Find all classes potentially marked with [MapGroup] ---
        var classDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // Quick filter: is it a class?
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            ) // Deeper filter: does it have the attribute?
            .Where(static m => m is not null)!; // Filter out classes that don't have the attribute

        // --- Step 2: Combine class information with compilation ---
        var compilationAndClasses = context.CompilationProvider.Combine(
            classDeclarations.Collect()
        );

        // --- Step 3: Register the source generation function ---
        context.RegisterSourceOutput(
            compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc)
        );
    }

    // --- Helper Methods for Initialization ---

    // Quick filter to check if a node is a ClassDeclarationSyntax
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax cdecl && cdecl.AttributeLists.Count > 0;

    // Deeper filter using semantic model to confirm the presence of [MapGroup]
    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(
        GeneratorSyntaxContext context
    )
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Iterate through attributes on the class
        foreach (var attributeListSyntax in classDeclaration.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (
                    context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol
                    is IMethodSymbol attributeSymbol
                )
                {
                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // Check if the attribute is [MapGroup]
                    if (fullName == MapGroupAttributeName)
                    {
                        return classDeclaration;
                    }
                }
            }
        }
        return null; // Not the attribute we're looking for
    }

    // --- Main Execution Logic ---

    private static void Execute(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax?> classes,
        SourceProductionContext context
    )
    {
        if (classes.IsDefaultOrEmpty)
        {
            // No classes marked with [MapGroup], nothing to do.
            return;
        }

        // Filter out potentially duplicate class declarations (e.g., partial classes)
        // We only need one declaration per class symbol
        var distinctClasses = classes
            .Select(c => compilation.GetSemanticModel(c.SyntaxTree).GetDeclaredSymbol(c))
            .Where(s => s is not null)
            .Distinct(SymbolEqualityComparer.Default)
            .OfType<INamedTypeSymbol>() // Ensure we have symbols
            .ToList();

        if (!distinctClasses.Any())
        {
            return;
        }
        
        // --- Collect Types for JSON Serialization ---
        var serializableTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        // --- Generate the code ---
        var (serviceExtensionCode, mappingExtensionCode) = GenerateExtensionMethods(
            compilation,
            distinctClasses,
            context
        );

        if (!string.IsNullOrEmpty(serviceExtensionCode))
        {
            context.AddSource("MinimalApiMapper.ServiceExtensions.g.cs", serviceExtensionCode);
        }
        if (!string.IsNullOrEmpty(mappingExtensionCode))
        {
            context.AddSource("MinimalApiMapper.MappingExtensions.g.cs", mappingExtensionCode);
        }
    }

    // --- Code Generation Implementation (Placeholder) ---

    private static (
        string? ServiceExtensionCode,
        string? MappingExtensionCode
    ) GenerateExtensionMethods(
        Compilation compilation,
        List<INamedTypeSymbol> groupClasses,
        SourceProductionContext context
    )
    {
        // TODO: Implement the actual code generation logic here
        // This will involve:
        // 1. Iterating through groupClasses.
        // 2. For each class, find methods marked with MapMethodAttribute or MapMethodsAttribute.
        // 3. Extract route info, parameters, etc. using the compilation and semantic models.
        // 4. Build the strings for the AddApiGroups and MapApiGroups extension methods.
        // 5. Handle errors and report diagnostics using context.ReportDiagnostic(...).

        var serviceBuilder = new StringBuilder();
        var mappingBuilder = new StringBuilder();
        var requiredUsings = new HashSet<string>(); // Track usings needed for mapping

        // Start building the service registration extension method (File-scoped namespace)
        serviceBuilder.AppendLine("// <auto-generated/>");
        serviceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        serviceBuilder.AppendLine();
        serviceBuilder.AppendLine("namespace MinimalApiMapper.Generated;"); // File-scoped
        serviceBuilder.AppendLine();
        serviceBuilder.AppendLine("public static class MinimalApiMapperServiceExtensions");
        serviceBuilder.AppendLine("{");
        serviceBuilder.AppendLine(
            "    public static IServiceCollection AddApiGroups(this IServiceCollection services)"
        );
        serviceBuilder.AppendLine("    {");

        // Start building the mapping extension method (File-scoped namespace)
        mappingBuilder.AppendLine("// <auto-generated/>");
        // Add base usings - more will be added later dynamically
        requiredUsings.Add("Microsoft.AspNetCore.Builder");
        requiredUsings.Add("Microsoft.AspNetCore.Http");
        requiredUsings.Add("Microsoft.AspNetCore.Routing");
        requiredUsings.Add("Microsoft.Extensions.DependencyInjection");
        requiredUsings.Add("System"); // For IServiceProvider, Task etc.

        // --- Loop through discovered API Group classes ---
        foreach (var groupClassSymbol in groupClasses)
        {
            var fullClassName = groupClassSymbol.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            requiredUsings.Add(groupClassSymbol.ContainingNamespace.ToDisplayString()); // Add namespace of the group class

            // Add service registration
            serviceBuilder.AppendLine($"        services.AddScoped<{fullClassName}>();");

            // Find the MapGroup attribute to get the prefix
            string groupPrefix = GetGroupPrefix(groupClassSymbol);

            // --- Loop through methods in the current group class ---
            foreach (var member in groupClassSymbol.GetMembers())
            {
                if (
                    member is IMethodSymbol methodSymbol
                    && !methodSymbol.IsStatic
                    // && !methodSymbol.IsConstructor
                    && methodSymbol.DeclaredAccessibility == Accessibility.Public
                )
                {
                    var methodAttributes = GetMappingAttributes(methodSymbol);
                    if (!methodAttributes.Any())
                        continue; // No mapping attributes found

                    foreach (var attrData in methodAttributes)
                    {
                        // Generate the mapping code snippet for this specific method and attribute
                        string? mappingSnippet = GenerateMappingForMethod(
                            compilation,
                            groupClassSymbol,
                            methodSymbol,
                            attrData,
                            groupPrefix,
                            requiredUsings,
                            context
                        );

                        if (mappingSnippet != null)
                        {
                            mappingBuilder.AppendLine(mappingSnippet);
                            mappingBuilder.AppendLine(); // Add a blank line for readability
                        }
                    }
                }
            }
        }

        // Finish building the service method
        serviceBuilder.AppendLine("        return services;");
        serviceBuilder.AppendLine("    }");
        serviceBuilder.AppendLine("}");

        // Assemble the final mapping code with usings
        var finalMappingCode = new StringBuilder();
        foreach (var ns in requiredUsings.OrderBy(u => u))
        {
            finalMappingCode.AppendLine($"using {ns};");
        }
        finalMappingCode.AppendLine();
        finalMappingCode.AppendLine("namespace MinimalApiMapper.Generated;"); // File-scoped
        finalMappingCode.AppendLine();
        finalMappingCode.AppendLine("public static class MinimalApiMapperMappingExtensions");
        finalMappingCode.AppendLine("{");
        finalMappingCode.AppendLine(
            "    public static IEndpointRouteBuilder MapApiGroups(this IEndpointRouteBuilder app)"
        );
        finalMappingCode.AppendLine("    {");
        finalMappingCode.Append(mappingBuilder.ToString()); // Append the generated mappings
        finalMappingCode.AppendLine("        return app;");
        finalMappingCode.AppendLine("    }");
        finalMappingCode.AppendLine("}");

        // Return the generated code
        var finalServiceCode = groupClasses.Any() ? serviceBuilder.ToString() : null;
        var finalMappingCodeStr = mappingBuilder.Length > 0 ? finalMappingCode.ToString() : null;

        return (finalServiceCode, finalMappingCodeStr);
    }

    // --- Helper Methods for Generation ---

    private static string GetGroupPrefix(INamedTypeSymbol groupClassSymbol)
    {
        var mapGroupAttr = groupClassSymbol
            .GetAttributes()
            .FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == MapGroupAttributeName);
        if (mapGroupAttr != null && mapGroupAttr.ConstructorArguments.Length > 0)
        {
            return mapGroupAttr.ConstructorArguments[0].Value?.ToString() ?? "";
        }
        return "";
    }

    private static List<AttributeData> GetMappingAttributes(IMethodSymbol methodSymbol)
    {
        return methodSymbol
            .GetAttributes()
            .Where(attr =>
                attr.AttributeClass != null
                && (
                    attr.AttributeClass.ToDisplayString() == MapMethodsAttributeName
                    || attr.AttributeClass.BaseType?.ToDisplayString() == MapMethodAttributeBaseName
                )
            )
            .ToList();
    }

    private static string? GenerateMappingForMethod(
        Compilation compilation,
        INamedTypeSymbol groupClassSymbol,
        IMethodSymbol methodSymbol,
        AttributeData mappingAttributeData,
        string groupPrefix,
        HashSet<string> requiredUsings,
        SourceProductionContext context
    )
    {
        // --- 1. Extract Mapping Attribute Info ---
        string? routeTemplate;
        List<string> httpMethods = new List<string>();
        string mappingAttributeFullName = mappingAttributeData.AttributeClass!.ToDisplayString();

        if (mappingAttributeFullName == MapMethodsAttributeName)
        {
            if (mappingAttributeData.ConstructorArguments.Length < 2)
                return null; // Invalid attribute usage
            routeTemplate = mappingAttributeData.ConstructorArguments[0].Value?.ToString();
            var methodsArray = mappingAttributeData.ConstructorArguments[1].Values;
            if (methodsArray.IsDefaultOrEmpty)
                return null; // No methods specified
            httpMethods.AddRange(
                methodsArray
                    .Select(m => m.Value?.ToString()?.ToUpperInvariant())
                    .Where(m => m != null)!
            );
        }
        else // One of the MapGet, MapPost etc. attributes
        {
            if (mappingAttributeData.ConstructorArguments.Length < 1)
                return null; // Invalid attribute usage
            routeTemplate = mappingAttributeData.ConstructorArguments[0].Value?.ToString();
            // Infer HTTP method from attribute name
            string attributeShortName = mappingAttributeData.AttributeClass!.Name; // e.g., "MapGetAttribute"
            string httpMethod = attributeShortName
                .Substring(3)
                .Replace("Attribute", "")
                .ToUpperInvariant(); // e.g., "GET"
            httpMethods.Add(httpMethod);
        }

        if (!httpMethods.Any())
            return null; // No valid HTTP methods found

        // --- 2. Combine Route ---
        string finalRoute = CombineRoute(groupPrefix, routeTemplate);

        // --- 3. Generate Attributes for the Lambda ---
        var lambdaAttributesBuilder = new StringBuilder();
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            // Skip our mapping attributes
            string attrFullName = attributeData.AttributeClass!.ToDisplayString();
            if (
                attrFullName == MapMethodsAttributeName
                || attributeData.AttributeClass.BaseType?.ToDisplayString()
                    == MapMethodAttributeBaseName
            )
            {
                continue;
            }

            string? attributeString = GenerateAttributeString(attributeData, requiredUsings);
            if (attributeString != null)
            {
                lambdaAttributesBuilder.Append(attributeString).Append(" "); // Add space after attribute
            }
        }
        string lambdaAttributes = lambdaAttributesBuilder.ToString();

        // --- 4. Analyze Parameters & Build Lambda Signature ---
        var lambdaParameters = new List<string>(); // e.g., "HttpContext context", "int id", "[FromBody] User user"
        var methodArguments = new List<string>(); // Arguments to pass to the actual method call

        // Instance methods still need HttpContext to resolve the group instance
        bool needsContextForInstance = !methodSymbol.IsStatic;
        bool contextParameterExists = methodSymbol.Parameters.Any(p =>
            p.Type.ToDisplayString() == "Microsoft.AspNetCore.Http.HttpContext"
        );

        if (needsContextForInstance && !contextParameterExists)
        {
            lambdaParameters.Add("HttpContext context");
            requiredUsings.Add("Microsoft.AspNetCore.Http");
        }

        foreach (var param in methodSymbol.Parameters)
        {
            string paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string paramName = param.Name;
            requiredUsings.Add(
                param.Type.ContainingNamespace.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                )
            ); // Ensure namespace is fully qualified

            var paramAttributesBuilder = new StringBuilder();
            foreach (var paramAttr in param.GetAttributes())
            {
                string? attributeString = GenerateAttributeString(paramAttr, requiredUsings);
                if (attributeString != null)
                {
                    paramAttributesBuilder.Append(attributeString).Append(" ");
                }
            }

            // Add parameter with its attributes and fully qualified type
            lambdaParameters.Add($"{paramAttributesBuilder}{paramType} {paramName}");
            methodArguments.Add(paramName); // Add param name for the call later
        }

        string lambdaSignature = string.Join(", ", lambdaParameters);

        // --- 4. Build Lambda Body ---
        var bodyBuilder = new StringBuilder();
        // string groupInstanceVar = $"{groupClassSymbol.Name.ToLowerInvariant()}Instance";
        string groupInstanceVar = $"{groupClassSymbol.Name.ToLowerInvariant()}_{Guid.NewGuid():N}"; // More unique name
        string fullGroupClassName = groupClassSymbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        if (!methodSymbol.IsStatic)
        {
            string contextAccess = contextParameterExists
                ? methodSymbol
                    .Parameters.First(p =>
                        p.Type.ToDisplayString() == "Microsoft.AspNetCore.Http.HttpContext"
                    )
                    .Name
                : "context";
            bodyBuilder.AppendLine($"            var sp = {contextAccess}.RequestServices;");
            bodyBuilder.AppendLine(
                $"            var {groupInstanceVar} = sp.GetRequiredService<{fullGroupClassName}>();"
            );
        }

        string methodCallArgs = string.Join(", ", methodArguments);
        string methodCallTarget = methodSymbol.IsStatic ? fullGroupClassName : groupInstanceVar;
        string methodCall = $"{methodCallTarget}.{methodSymbol.Name}({methodCallArgs})";

        // Handle async methods
        bool isAsync =
            methodSymbol.ReturnType is INamedTypeSymbol returnType
            && (
                returnType.GetFullName() == "System.Threading.Tasks.Task"
                || returnType.GetFullName() == "System.Threading.Tasks.ValueTask"
                || (
                    returnType.IsGenericType
                    && (
                        returnType.ConstructedFrom.GetFullName() == "System.Threading.Tasks.Task"
                        || returnType.ConstructedFrom.GetFullName()
                            == "System.Threading.Tasks.ValueTask"
                    )
                )
            );
        
        // Add return type namespace
        if(methodSymbol.ReturnType is INamedTypeSymbol namedReturn && !methodSymbol.ReturnsVoid)
        {
            requiredUsings.Add(namedReturn.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            if(namedReturn.IsGenericType)
            {
                foreach(var typeArg in namedReturn.TypeArguments)
                {
                    if(typeArg is INamedTypeSymbol namedTypeArg)
                    {
                        requiredUsings.Add(namedTypeArg.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    }
                }
            }
        }

        if (isAsync)
        {
            bodyBuilder.Append($"            return await {methodCall};");
        }
        else if (methodSymbol.ReturnsVoid)
        {
            bodyBuilder.AppendLine($"            {methodCall};");
            bodyBuilder.Append($"            return Results.Ok();"); // Default for void
            requiredUsings.Add("Microsoft.AspNetCore.Http"); // For Results
        }
        else // Synchronous method returning a value
        {
            bodyBuilder.Append($"            return {methodCall};");
        }

        // --- 6. Generate app.MapX Call ---
        string mapMethodName = DetermineMapMethod(httpMethods);
        string httpMethodsArg = mapMethodName == "MapMethods"
            ? $", new [] {{ {string.Join(", ", httpMethods.Select(m => $"\"{m}\""))} }}"
            : "";

        string lambdaModifier = isAsync ? "async " : "";
        
        var snippet = new StringBuilder();
        // Append method attributes before the lambda signature
        snippet.Append($"        app.{mapMethodName}(\"{finalRoute}\", {lambdaAttributes}{lambdaModifier}({lambdaSignature}) =>");
        snippet.AppendLine(); // Start lambda body on new line
        snippet.AppendLine( "        {");
        snippet.AppendLine(bodyBuilder.ToString());
        snippet.Append( "        })");
        // TODO: Add fluent API calls here later if needed (.WithName(), .WithTags(), etc.)
        snippet.Append(";");

        return snippet.ToString();
    }

    // --- New Helper Method for Generating Attribute Strings ---
    private static string? GenerateAttributeString(
        AttributeData attributeData,
        HashSet<string> requiredUsings
    )
    {
        if (attributeData.AttributeClass == null)
            return null;

        var attributeType = attributeData.AttributeClass;
        string fullAttributeTypeName = attributeType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );
        requiredUsings.Add(
            attributeType.ContainingNamespace.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            )
        );

        var arguments = new List<string>();

        // Constructor arguments
        foreach (var arg in attributeData.ConstructorArguments)
        {
            arguments.Add(FormatTypedConstant(arg, requiredUsings));
        }

        // Named arguments
        foreach (var arg in attributeData.NamedArguments)
        {
            arguments.Add($"{arg.Key} = {FormatTypedConstant(arg.Value, requiredUsings)}");
        }

        string argsString = arguments.Any() ? $"({string.Join(", ", arguments)})" : "";
        return $"[{fullAttributeTypeName}{argsString}]";
    }

    // --- New Helper Method for Formatting TypedConstant Values ---
    private static string FormatTypedConstant(
        TypedConstant constant,
        HashSet<string> requiredUsings
    )
    {
        if (constant.IsNull)
            return "null";

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                if (constant.Value is string s)
                    return $"\"{s.Replace("\"", "\\\"")}\""; // Escape quotes
                if (constant.Value is bool b)
                    return b ? "true" : "false";
                if (constant.Value is char c)
                    return $"'{c}'";
                // Add handling for other primitives if necessary (float, double literals)
                return constant.Value?.ToString() ?? "null";

            case TypedConstantKind.Enum:
                if (constant.Type is INamedTypeSymbol enumType)
                {
                    requiredUsings.Add(
                        enumType.ContainingNamespace.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        )
                    );
                    // Find the enum member name matching the value
                    var memberName = enumType
                        .GetMembers()
                        .OfType<IFieldSymbol>()
                        .FirstOrDefault(f =>
                            f.ConstantValue != null && f.ConstantValue.Equals(constant.Value)
                        )
                        ?.Name;
                    if (memberName != null)
                    {
                        return $"{enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{memberName}";
                    }
                }
                // Fallback to casting the underlying value
                return $"({constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({constant.Value})";

            case TypedConstantKind.Type:
                if (constant.Value is ITypeSymbol typeSymbol)
                {
                    requiredUsings.Add(
                        typeSymbol.ContainingNamespace.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        )
                    );
                    return $"typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
                }
                return "null"; // Should not happen?

            case TypedConstantKind.Array:
                var arrayValues = constant.Values.Select(v =>
                    FormatTypedConstant(v, requiredUsings)
                );
                string arrayTypeName = constant.Type is IArrayTypeSymbol ats
                    ? ats.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : "object"; // Fallback type
                requiredUsings.Add(
                    (
                        constant.Type as IArrayTypeSymbol
                    )?.ElementType.ContainingNamespace.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    ) ?? "System"
                );
                return $"new {arrayTypeName}[] {{ {string.Join(", ", arrayValues)} }}";

            default:
                return constant.Value?.ToString() ?? "null"; // Fallback
        }
    }

    private static string CombineRoute(string prefix, string? template)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return template?.TrimStart('/') ?? "";
        }
        if (string.IsNullOrWhiteSpace(template))
        {
            return prefix;
        }
        return $"{prefix}/{template.TrimStart('/')}";
    }

    private static string DetermineMapMethod(List<string> httpMethods)
    {
        if (httpMethods.Count == 1)
        {
            switch (httpMethods[0])
            {
                case "GET":
                    return "MapGet";
                case "POST":
                    return "MapPost";
                case "PUT":
                    return "MapPut";
                case "DELETE":
                    return "MapDelete";
                case "PATCH":
                    return "MapPatch";
            }
        }
        // Fallback for multiple methods or less common single methods
        return "MapMethods";
    }
}
