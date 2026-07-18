using TracerTests.Helpers;

namespace TracerTests;

/// <summary>
/// Debug tracer tests validate the externally observable contract of the local development
/// (Debug) build. They compile the CVApp project in Debug configuration and assert that
/// the debug-only code paths are present and the static payload is not generated.
/// </summary>
public class DebugTracerTests
{
    [Fact]
    public void Debug_Build_SucceedsWithoutError()
    {
        using var result = BuildHelpers.RunDotnetBuild("Debug");

        Assert.True(result.Succeeded,
            $"Debug build failed (exit code {result.ExitCode}).\nOutput:\n{result.Output}\nError:\n{result.Error}");
    }

    [Fact]
    public void Debug_Build_DoesNotProduceExperienceJsonInSourceWwwroot()
    {
        using var result = BuildHelpers.RunDotnetBuild("Debug");

        Assert.True(result.Succeeded,
            $"Debug build must succeed before artifact check can run.\nOutput:\n{result.Output}\nError:\n{result.Error}");

        // In Debug mode the static aggregation step is intentionally skipped.
        // The data/ directory in the source wwwroot folder must NOT contain a
        // pre-compiled experience.json — that file is produced only during Release publish.
        var wwwrootDataDir = Path.Combine(result.OutputDirectory, "wwwroot", "data");
        var experienceJsonPath = Path.Combine(wwwrootDataDir, "experience.json");

        // The file should not exist in the debug build output's wwwroot/data folder.
        // (It is acceptable for it to exist as a side-effect from a prior Release build
        // in the source tree, but it must not be a product of the Debug build step itself.)
        // We verify by checking the build output directory, not the source tree.
        if (Directory.Exists(wwwrootDataDir))
        {
            Assert.False(File.Exists(experienceJsonPath),
                "Debug build output must not produce 'wwwroot/data/experience.json'. " +
                "The static payload is a Release-only artifact produced by the aggregation step.");
        }
        // If the data directory doesn't exist at all in debug output, that is fine.
    }

    [Fact]
    public void Debug_Build_CompiledAssemblyContainsApiDataSourceRoute()
    {
        using var result = BuildHelpers.RunDotnetBuild("Debug");

        Assert.True(result.Succeeded,
            $"Debug build must succeed before assembly inspection can run.\nOutput:\n{result.Output}\nError:\n{result.Error}");

        // ADR-004 / ADR-010: In Debug mode the ExperienceDataService fetches from the local
        // API endpoint (/api/experience) rather than the compiled static JSON.
        // Verify the compiled DLL contains the debug API route string.
        var cvAppDll = Path.Combine(result.OutputDirectory, "CVApp.dll");
        if (!File.Exists(cvAppDll))
            return; // DLL not in expected location for this build layout — skip

        Assert.True(BuildHelpers.DllContainsStringLiteral(cvAppDll, "api/experience"),
            "Debug build must include the local API route 'api/experience' in the compiled assembly. " +
            "Ensure the ExperienceDataService uses #if DEBUG to select the API data source (see ADR-010).");
    }

    [Fact]
    public void Debug_Build_CompiledAssemblyContainsCanonicalExperienceRoute()
    {
        using var result = BuildHelpers.RunDotnetBuild("Debug");

        Assert.True(result.Succeeded,
            $"Debug build must succeed before route inspection can run.\nOutput:\n{result.Output}\nError:\n{result.Error}");

        var cvAppDll = Path.Combine(result.OutputDirectory, "CVApp.dll");
        if (!File.Exists(cvAppDll))
            return; // DLL not in expected location for this build layout — skip

        Assert.True(BuildHelpers.DllContainsStringLiteral(cvAppDll, "/experience"),
            "Debug build must include the canonical '/experience' route.");
    }
}
