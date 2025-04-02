using Microsoft.CodeAnalysis;
using MinimalApiMapper.SourceGenerator.Extensions;

namespace MinimalApiMapper.SourceGenerator.Helpers;

internal static class SerializableTypes
{
    private static void AddTypeAndGenericArguments(
        ITypeSymbol type,
        HashSet<ITypeSymbol> serializableTypes
    )
    {
        if (
            type == null
            || type is IErrorTypeSymbol
            || type.SpecialType != SpecialType.None
            || type.TypeKind == TypeKind.Interface
            || type.TypeKind == TypeKind.TypeParameter
            || type.IsAbstract
        )
            return; // Skip errors, primitives, interfaces, generics placeholders, abstract classes

        // Handle arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            AddTypeAndGenericArguments(arrayType.ElementType, serializableTypes);
            return; // Don't add array type itself, STJ handles T[] if T is known
        }

        // Handle Nullable<T> - add T
        if (
            type is INamedTypeSymbol nullableType
            && nullableType.IsGenericType
            && nullableType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
        )
        {
            AddTypeAndGenericArguments(nullableType.TypeArguments[0], serializableTypes);
            return;
        }

        // Check if already added
        if (!serializableTypes.Add(type))
            return; // Use Add method's return value

        // If it's a generic type, add its type arguments recursively
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                AddTypeAndGenericArguments(typeArg, serializableTypes);
            }
        }

        // Recursively add types of public properties (needed for STJ source gen)
        foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
        {
            // Only public properties with getters that aren't explicitly ignored
            if (
                member.DeclaredAccessibility == Accessibility.Public
                && member.GetMethod != null
                && !member
                    .GetAttributes()
                    .Any(ad =>
                        ad.AttributeClass?.GetFullName()
                        == "System.Text.Json.Serialization.JsonIgnoreAttribute"
                    )
            )
            {
                AddTypeAndGenericArguments(member.Type, serializableTypes);
            }
        }
        // Also consider public fields? Usually less common for DTOs.
    }

    public static void CollectSerializableTypesFromMethod(
        IMethodSymbol methodSymbol,
        HashSet<ITypeSymbol> serializableTypes
    )
    {
        // Check parameters
        foreach (var param in methodSymbol.Parameters)
        {
            bool isFromBody = param
                .GetAttributes()
                .Any(ad => ad.AttributeClass?.Name == "FromBodyAttribute");
            // Consider FromBody or complex types (not primitive/framework)
            if (isFromBody || (!param.Type.IsPrimitiveType() && !param.Type.IsFrameworkType()))
            {
                AddTypeAndGenericArguments(param.Type, serializableTypes);
            }
        }

        // Check return type
        ITypeSymbol? actualType = GetActualReturnType(methodSymbol.ReturnType);
        if (actualType != null) // GetActualReturnType already filters framework/primitive
        {
            AddTypeAndGenericArguments(actualType, serializableTypes);
        }
    }

    private static ITypeSymbol? GetActualReturnType(ITypeSymbol returnType)
    {
        if (returnType == null || returnType is IErrorTypeSymbol)
            return null;

        // Handle Task<T>, ValueTask<T>
        if (returnType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            string fullName = namedType.ConstructedFrom.GetFullName(); // Use extension method
            if (
                fullName == "global::System.Threading.Tasks.Task<TResult>"
                || fullName == "global::System.Threading.Tasks.ValueTask<TResult>"
            )
            {
                return GetActualReturnType(namedType.TypeArguments[0]);
            }
            // Handle ActionResult<T>
            if (fullName == "global::Microsoft.AspNetCore.Mvc.ActionResult<TValue>")
            {
                return GetActualReturnType(namedType.TypeArguments[0]);
            }
        }

        // Handle IResult types (like Results<Ok<T>, NotFound>)
        if (
            returnType is INamedTypeSymbol resultsType
            && resultsType.ContainingNamespace.GetFullName() == "global::Microsoft.AspNetCore.Http"
            && returnType.Name.StartsWith("Results")
            && resultsType.IsGenericType
        )
        {
            ITypeSymbol? foundType = null;
            foreach (var arg in resultsType.TypeArguments)
            {
                // Recurse on the argument type itself (e.g., Ok<User>)
                var unwrappedArg = GetActualReturnType(arg);
                if (unwrappedArg != null)
                {
                    // Prefer the first non-null unwrapped type found
                    foundType = unwrappedArg;
                    break;
                }
            }
            if (foundType != null)
                return foundType;
        }

        // Handle specific IResult implementations (Ok<T>, Created<T>, etc.)
        if (
            returnType is INamedTypeSymbol resultType
            && resultType.IsGenericType
            && resultType
                .ContainingNamespace.GetFullName()
                .StartsWith("global::Microsoft.AspNetCore.Http.HttpResults")
        )
        {
            string genericTypeDef = resultType.ConstructedFrom.GetFullName();
            if (
                genericTypeDef == "global::Microsoft.AspNetCore.Http.HttpResults.Ok<TValue>"
                || genericTypeDef == "global::Microsoft.AspNetCore.Http.HttpResults.Created<TValue>"
                || genericTypeDef
                    == "global::Microsoft.AspNetCore.Http.HttpResults.Accepted<TValue>"
                || genericTypeDef
                    == "global::Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<TValue>"
            )
            {
                return GetActualReturnType(resultType.TypeArguments[0]);
            }
        }

        // If it's void, Task, ValueTask, IResult, IActionResult etc. - return null
        if (
            returnType.SpecialType == SpecialType.System_Void
            || returnType.GetFullName() == "global::System.Threading.Tasks.Task"
            || returnType.GetFullName() == "global::System.Threading.Tasks.ValueTask"
            || returnType.AllInterfaces.Any(i =>
                i.GetFullName() == "global::Microsoft.AspNetCore.Http.IResult"
            )
            || returnType.AllInterfaces.Any(i =>
                i.GetFullName() == "global::Microsoft.AspNetCore.Mvc.IActionResult"
            )
        )
        {
            return null;
        }

        // If it's a framework type, primitive, interface, abstract - return null
        if (
            returnType.IsFrameworkType()
            || returnType.IsPrimitiveType()
            || returnType.TypeKind == TypeKind.Interface
            || returnType.IsAbstract
        )
        {
            return null;
        }

        return returnType; // Return the type if it's potentially serializable
    }

    // Helper to get all required namespaces for a type and its generics/properties
    public static IEnumerable<string> GetNamespacesForType(this ITypeSymbol? type)
    {
        if (
            type == null
            || type is IErrorTypeSymbol
            || type.ContainingNamespace == null
            || type.ContainingNamespace.IsGlobalNamespace
        )
            yield break;

        yield return type.ContainingNamespace.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            foreach (var arg in namedType.TypeArguments)
            {
                foreach (var ns in GetNamespacesForType(arg))
                    yield return ns;
            }
        }
        // Add namespaces from properties too? Not strictly needed for JsonSerializable attribute itself.
    }
}
