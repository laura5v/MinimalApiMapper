using System.Reflection;

namespace MinimalApiMapper.SourceGenerator.Helpers;

internal class CodeBuilder
{
    public static string GetGeneratedCodeAttribute(Assembly? assembly = null)
    {
        assembly ??= typeof(CodeBuilder).Assembly;
        
        var name = assembly.GetName();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = versionAttribute.InformationalVersion;
        
        // Strip hash
        if (version.Contains("+"))
        {
            version = version[..version.IndexOf("+", StringComparison.Ordinal)];
        }
        
        return $"[System.CodeDom.Compiler.GeneratedCode(\"{name.Name}\", \"{version}\")]";
    }
}
