using System.Diagnostics;
using System.Reflection;

namespace TracerTests.Helpers;

public static class BuildHelpers
{
    private static readonly string CVAppProjectPath;
    private static readonly string RepositoryRoot;

    static BuildHelpers()
    {
        var assembly = typeof(BuildHelpers).Assembly;
        CVAppProjectPath = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "CVAppProjectPath")?.Value
            ?? throw new InvalidOperationException("CVAppProjectPath assembly metadata not set. Ensure the TracerTests.csproj defines the AssemblyMetadata for 'CVAppProjectPath'.");
        RepositoryRoot = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RepositoryRoot")?.Value
            ?? throw new InvalidOperationException("RepositoryRoot assembly metadata not set. Ensure the TracerTests.csproj defines the AssemblyMetadata for 'RepositoryRoot'.");
    }

    public static BuildResult RunDotnetBuild(string configuration, string? extraArgs = null)
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"cv-tracer-build-{Guid.NewGuid():N}");
        var args = $"build \"{CVAppProjectPath}\" -c {configuration} --nologo -o \"{outputDir}\"";
        if (!string.IsNullOrEmpty(extraArgs))
            args += $" {extraArgs}";

        var result = RunDotnet(args, RepositoryRoot);
        return new BuildResult(result.ExitCode, result.Output, result.Error, outputDir);
    }

    public static BuildResult RunDotnetPublish(string configuration, string? extraArgs = null)
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"cv-tracer-publish-{Guid.NewGuid():N}");
        var args = $"publish \"{CVAppProjectPath}\" -c {configuration} --nologo -o \"{outputDir}\"";
        if (!string.IsNullOrEmpty(extraArgs))
            args += $" {extraArgs}";

        var result = RunDotnet(args, RepositoryRoot);
        return new BuildResult(result.ExitCode, result.Output, result.Error, outputDir);
    }

    private static (int ExitCode, string Output, string Error) RunDotnet(string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet process.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output, error);
    }

    /// <summary>
    /// Searches a compiled .NET IL assembly for a string literal.
    /// .NET IL assemblies store string literals in the #US metadata heap as UTF-16LE.
    /// </summary>
    public static bool DllContainsStringLiteral(string dllPath, string searchString)
    {
        var dllBytes = File.ReadAllBytes(dllPath);
        var needle = System.Text.Encoding.Unicode.GetBytes(searchString);
        var dllSpan = dllBytes.AsSpan();
        var needleSpan = needle.AsSpan();

        for (var i = 0; i <= dllSpan.Length - needleSpan.Length; i++)
        {
            if (dllSpan.Slice(i, needleSpan.Length).SequenceEqual(needleSpan))
                return true;
        }
        return false;
    }
}

public record BuildResult(int ExitCode, string Output, string Error, string OutputDirectory) : IDisposable
{
    public bool Succeeded => ExitCode == 0;

    public void Dispose()
    {
        if (Directory.Exists(OutputDirectory))
            Directory.Delete(OutputDirectory, recursive: true);
    }
}
