using Microsoft.CodeAnalysis;

namespace MinimalApiMapper.SourceGenerator;

internal static class TypeSymbolExtensions
{
    // Helper to get full name robustly (needed for Task/ValueTask check)
    public static string GetFullName(this ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
