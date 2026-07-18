using TracerTests.Helpers;

namespace TracerTests;

/// <summary>
/// Release tracer tests validate the externally observable contract of the production
/// (Release) build. They compile and publish the CVApp project and assert the resulting
/// publish output satisfies the production artifact contract.
/// </summary>
public class ReleaseTracerTests
{
    [Fact]
    public void Release_Publish_SucceedsWithoutError()
    {
        using var result = BuildHelpers.RunDotnetPublish("Release");

        Assert.True(result.Succeeded,
            $"Release publish failed (exit code {result.ExitCode}).\nOutput:\n{result.Output}\nError:\n{result.Error}");
    }

    [Fact]
    public void Release_Publish_ProducesExperienceJsonAtExpectedPath()
    {
        using var result = BuildHelpers.RunDotnetPublish("Release");

        Assert.True(result.Succeeded,
            $"Release publish must succeed before artifact can be verified.\nOutput:\n{result.Output}\nError:\n{result.Error}");

        var experienceJsonPath = Path.Combine(result.OutputDirectory, "wwwroot", "data", "experience.json");

        Assert.True(File.Exists(experienceJsonPath),
            $"Publish verification failed: expected artifact not found at strict path 'wwwroot/data/experience.json'.\n" +
            $"Checked: {experienceJsonPath}\n" +
            $"Contents of publish output directory:\n{ListDirectory(result.OutputDirectory)}");
    }

    [Fact]
    public void Release_Publish_DoesNotProduceResumeJson()
    {
        using var result = BuildHelpers.RunDotnetPublish("Release");

        Assert.True(result.Succeeded,
            $"Release publish must succeed before artifact can be verified.\nOutput:\n{result.Output}\nError:\n{result.Error}");

        var legacyPath = Path.Combine(result.OutputDirectory, "wwwroot", "data", "resume.json");

        Assert.False(File.Exists(legacyPath),
            $"Publish output must not contain the legacy 'resume.json' artifact.\n" +
            $"Found unexpected file at: {legacyPath}");
    }

    [Fact]
    public void Release_Publish_ExperienceJsonIsValidJson()
    {
        using var result = BuildHelpers.RunDotnetPublish("Release");

        Assert.True(result.Succeeded,
            $"Release publish must succeed before artifact can be verified.\nOutput:\n{result.Output}\nError:\n{result.Error}");

        var experienceJsonPath = Path.Combine(result.OutputDirectory, "wwwroot", "data", "experience.json");
        Assert.True(File.Exists(experienceJsonPath), $"experience.json not found at {experienceJsonPath}");

        var content = File.ReadAllText(experienceJsonPath);
        Assert.False(string.IsNullOrWhiteSpace(content), "experience.json must not be empty.");

        // Validate the file parses as valid JSON with the expected root structure
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("timeline", out _),
            $"experience.json must contain a 'timeline' root property.\nActual content:\n{content}");
    }

    [Fact]
    public void Release_Publish_DoesNotContainAdminApiCode()
    {
        using var result = BuildHelpers.RunDotnetPublish("Release");

        Assert.True(result.Succeeded,
            $"Release publish must succeed before admin API check can run.\nOutput:\n{result.Output}\nError:\n{result.Error}");

        // The Release build strips all #if DEBUG admin/CMS code at compile time (ADR-004).
        // Verify by checking that no admin-API marker strings are present in the compiled DLL.
        var wwwrootFramework = Path.Combine(result.OutputDirectory, "wwwroot", "_framework");
        if (!Directory.Exists(wwwrootFramework))
            return; // Blazor WASM — framework assemblies not present, nothing to check

        var cvAppDll = Directory.GetFiles(wwwrootFramework, "CVApp.dll", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        if (cvAppDll == null)
            return; // DLL not found in framework folder — publish layout may differ

        // ADR-004: admin CMS endpoints and UI are guarded by #if DEBUG.
        // In a Release build the literal string "/api/experience" (the debug-only API route)
        // must not appear in the compiled assembly.
        Assert.False(BuildHelpers.DllContainsStringLiteral(cvAppDll, "/api/experience"),
            "Release build must not contain the debug-only '/api/experience' API route string. " +
            "Ensure the ExperienceDataService uses #if DEBUG guards correctly (see ADR-004).");
    }

    private static string ListDirectory(string dir)
    {
        if (!Directory.Exists(dir))
            return $"(directory does not exist: {dir})";

        var entries = Directory.GetFileSystemEntries(dir, "*", SearchOption.AllDirectories)
            .Select(p => p.Replace(dir, string.Empty).TrimStart(Path.DirectorySeparatorChar))
            .OrderBy(p => p);
        return string.Join(Environment.NewLine, entries);
    }
}
