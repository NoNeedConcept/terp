using System.Runtime.CompilerServices;

namespace Terp.API.Tests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyDiffPlex.Initialize();
        UseProjectRelativeDirectory("verify");
        VerifierSettings.UniqueForRuntime();
        VerifierSettings.InitializePlugins();
    }
}
