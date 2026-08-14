using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Playwright;

namespace SmokeTests;

public class ReleaseSmokeTests
{
    [Fact]
    public async Task ReleasePublishArtifacts_LoadInHostedCvPath_WithoutBlazorErrorUi()
    {
        using var publish = RunDotnetPublishRelease();
        Assert.True(publish.Succeeded,
            $"Release publish failed (exit code {publish.ExitCode}).\nOutput:\n{publish.Output}\nError:\n{publish.Error}");

        var publishWwwroot = Path.Combine(publish.OutputDirectory, "wwwroot");
        var indexHtmlPath = Path.Combine(publishWwwroot, "index.html");
        Assert.True(File.Exists(indexHtmlPath), $"Expected published index.html at {indexHtmlPath}");

        var indexHtml = await File.ReadAllTextAsync(indexHtmlPath);
        Assert.Contains("<base href=\"/CV/\" />", indexHtml);

        await using var harness = await CvStaticHarness.StartAsync(publishWwwroot);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        var failedRequests = new List<string>();
        var pageErrors = new List<string>();

        page.RequestFailed += (_, request) =>
        {
            failedRequests.Add($"{request.Method} {request.Url} ({request.Failure})");
        };

        page.PageError += (_, exception) =>
        {
            pageErrors.Add(exception);
        };

        var response = await page.GotoAsync($"{harness.BaseUrl}/CV/", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response!.Status);
        Assert.Contains("Hello, world!", await page.TextContentAsync("h1"));

        Assert.Empty(pageErrors);
        Assert.Empty(failedRequests.Where(r => r.Contains("/CV/_framework/", StringComparison.OrdinalIgnoreCase)));

        var blazorErrorUiVisible = await page.Locator("#blazor-error-ui").IsVisibleAsync();
        Assert.False(blazorErrorUiVisible, "Blazor error UI is visible, indicating app startup failure.");
    }

    private static BuildResult RunDotnetPublishRelease()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"cv-smoke-publish-{Guid.NewGuid():N}");
        var args =
            $"publish \"{GetAssemblyMetadata(\"CVAppProjectPath\")}\" -c Release --nologo -o \"{outputDir}\" -p:ReleaseBaseHref=/CV/";
        var result = RunDotnet(args, GetAssemblyMetadata("RepositoryRoot"));
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

    private static string GetAssemblyMetadata(string key)
    {
        return typeof(ReleaseSmokeTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value
               ?? throw new InvalidOperationException(
                   $"Assembly metadata '{key}' not found. Ensure SmokeTests.csproj defines it.");
    }
}

file sealed class CvStaticHarness : IAsyncDisposable
{
    private readonly WebApplication _app;

    private CvStaticHarness(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }

    public static async Task<CvStaticHarness> StartAsync(string publishedWwwroot)
    {
        var builder = WebApplication.CreateBuilder();
        var port = GetOpenPort();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        var app = builder.Build();

        app.MapGet("/", context =>
        {
            context.Response.Redirect("/CV/");
            return Task.CompletedTask;
        });

        app.Map("/CV", cvApp =>
        {
            var fileProvider = new PhysicalFileProvider(publishedWwwroot);
            cvApp.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider
            });

            cvApp.Run(async context =>
            {
                var path = context.Request.Path.Value ?? string.Empty;
                if (Path.HasExtension(path))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync(Path.Combine(publishedWwwroot, "index.html"));
            });
        });

        await app.StartAsync();
        return new CvStaticHarness(app, $"http://127.0.0.1:{port}");
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private static int GetOpenPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

file sealed record BuildResult(int ExitCode, string Output, string Error, string OutputDirectory) : IDisposable
{
    public bool Succeeded => ExitCode == 0;

    public void Dispose()
    {
        if (Directory.Exists(OutputDirectory))
            Directory.Delete(OutputDirectory, recursive: true);
    }
}
