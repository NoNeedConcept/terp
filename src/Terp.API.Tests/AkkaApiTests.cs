using PublicApiGenerator;

namespace Terp.API.Tests;

public sealed class AkkaApiTests
{
    private static readonly ApiGeneratorOptions _apiOptions = new()
    {
        ExcludeAttributes =
        [
            // Build-/CI-dependent metadata (RepositoryUrl from PublishRepositoryUrl) must not end up in the snapshot
            "System.Reflection.AssemblyMetadataAttribute",
            "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute",
            "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
            "System.Runtime.CompilerServices.IteratorStateMachineAttribute"
        ]
    };

    private static Task VerifyAssembly(Type type)
    {
        return Verify(type.Assembly.GeneratePublicApi(_apiOptions));
    }

    [Fact]
    public Task PublicApi_AkkaAssembly_MatchesApprovedSnapshot()
    {
        return VerifyAssembly(typeof(Terp.Akka.ResolveExtensions));
    }
}
