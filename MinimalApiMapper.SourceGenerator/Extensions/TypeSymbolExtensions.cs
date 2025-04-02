using Microsoft.CodeAnalysis;

namespace MinimalApiMapper.SourceGenerator.Extensions;

internal static class TypeSymbolExtensions
{
    // Basic primitive check
    public static bool IsPrimitiveType(this ITypeSymbol type)
    {
        return type.SpecialType is >= SpecialType.System_Boolean and <= SpecialType.System_Double;
    }
    
    // Basic framework type check (expand as needed)
    public static bool IsFrameworkType(this ITypeSymbol? type)
    {
        if (type?.ContainingNamespace == null) 
            return false;
        
        var ns = type.ContainingNamespace.ToDisplayString();
        
        return ns == "System" ||
               ns.StartsWith("System.") ||
               ns == "Microsoft.AspNetCore.Http" ||
               ns.StartsWith("Microsoft.AspNetCore.Http.") ||
               ns == "Microsoft.AspNetCore.Mvc" ||
               ns.StartsWith("Microsoft.AspNetCore.Mvc.") ||
               ns == "Microsoft.Extensions.Logging"; // Add other common framework namespaces
    }
    
    // Helper to get full name robustly (needed for Task/ValueTask check)
    public static string GetFullName(this ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
    
    public static string GetFullName(this INamespaceSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
