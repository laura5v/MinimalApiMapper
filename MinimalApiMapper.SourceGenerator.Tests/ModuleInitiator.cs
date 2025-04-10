using System.Runtime.CompilerServices;

namespace MinimalApiMapper.SourceGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        // Initializes Verify Source Generators globally
        VerifySourceGenerators.Initialize();
        
        UseProjectRelativeDirectory("Snapshots");
    }
}
