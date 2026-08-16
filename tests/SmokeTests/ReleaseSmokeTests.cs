using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Playwright;

namespace SmokeTests;

public class ReleaseSmokeTests
{
    [Fact]
    public async Task ReleasePublishArtifacts_RenderProfileHeaderAtRootAndExperienceRoutes()
    {
        await AssertPublishedSiteRendersAsync("/CV/");
    }

    [Fact]
    public async Task ReleasePublishArtifacts_RenderProfileHeaderAtPreviewRoutes()
    {
        await AssertPublishedSiteRendersAsync("/CV/preview/pr-42/");
    }

    [Fact]
    public async Task ReleasePublishArtifacts_PrintConfigModalActionsReachableOnMobileViewport()
    {
        const string baseHref = "/CV/";

        using var publish = await RunDotnetPublishReleaseAsync(baseHref);
        Assert.True(publish.Succeeded,
            $"Release publish failed (exit code {publish.ExitCode}).\nOutput:\n{publish.Output}\nError:\n{publish.Error}");

        var routePrefix = baseHref.TrimEnd('/');
        var publishWwwroot = Path.Combine(publish.OutputDirectory, "wwwroot");
        await using var harness = await CvStaticHarness.StartAsync(publishWwwroot, routePrefix);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = 390,
                Height = 640
            }
        });

        await page.GotoAsync($"{harness.BaseUrl}{baseHref}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.GetByRole(AriaRole.Button, new() { Name = "Print / Save PDF" }).ClickAsync();
        var content = page.Locator(".print-config-modal__content");
        await content.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        var generateButton = page.GetByRole(AriaRole.Button, new() { Name = "Generate / Print" });
        await generateButton.ScrollIntoViewIfNeededAsync();

        Assert.True(await IsFullyWithinViewportAsync(generateButton),
            "Expected the print modal action buttons to be reachable on mobile after scrolling the modal content.");
    }

    private static async Task AssertPublishedSiteRendersAsync(string baseHref)
    {
        using var publish = await RunDotnetPublishReleaseAsync(baseHref);
        Assert.True(publish.Succeeded,
            $"Release publish failed (exit code {publish.ExitCode}).\nOutput:\n{publish.Output}\nError:\n{publish.Error}");

        var publishWwwroot = Path.Combine(publish.OutputDirectory, "wwwroot");
        var indexHtmlPath = Path.Combine(publishWwwroot, "index.html");
        Assert.True(File.Exists(indexHtmlPath), $"Expected published index.html at {indexHtmlPath}");

        var indexHtml = await File.ReadAllTextAsync(indexHtmlPath);
        Assert.Contains($"<base href=\"{baseHref}\" />", indexHtml);

        var routePrefix = baseHref.TrimEnd('/');
        await using var harness = await CvStaticHarness.StartAsync(publishWwwroot, routePrefix);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        var failedRequests = new List<string>();
        var pageErrors = new List<string>();
        var consoleErrors = new List<string>();

        page.RequestFailed += (_, request) =>
        {
            failedRequests.Add($"{request.Method} {request.Url} ({request.Failure})");
        };

        page.PageError += (_, exception) =>
        {
            pageErrors.Add(exception);
        };

        page.Console += (_, message) =>
        {
            if (message.Type == "error")
                consoleErrors.Add(message.Text);
        };

        await AssertProfileHeaderAsync(page, $"{harness.BaseUrl}{baseHref}", pageErrors, consoleErrors, failedRequests);
        await AssertProfileHeaderAsync(page, $"{harness.BaseUrl}{routePrefix}/experience", pageErrors, consoleErrors, failedRequests);

        Assert.Empty(pageErrors);
        Assert.Empty(consoleErrors);
        Assert.Empty(failedRequests.Where(r => r.Contains($"{routePrefix}/_framework/", StringComparison.OrdinalIgnoreCase)));

        var blazorErrorUiVisible = await page.Locator("#blazor-error-ui").IsVisibleAsync();
        Assert.False(blazorErrorUiVisible, "Blazor error UI is visible, indicating app startup failure.");
    }

    private static async Task AssertProfileHeaderAsync(
        IPage page,
        string url,
        List<string> pageErrors,
        List<string> consoleErrors,
        List<string> failedRequests)
    {
        var response = await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response!.Status);

        var heading = page.Locator("h1").First;
        try
        {
            await heading.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 120_000
            });
        }
        catch (TimeoutException)
        {
            var pageHtml = await page.ContentAsync();
            var diagnostics = string.Join(Environment.NewLine, new[]
            {
                $"Smoke test failed to render heading at {url} within 120 seconds.",
                $"Page errors: {(pageErrors.Count == 0 ? "(none)" : string.Join(" | ", pageErrors))}",
                $"Console errors: {(consoleErrors.Count == 0 ? "(none)" : string.Join(" | ", consoleErrors))}",
                $"Failed requests: {(failedRequests.Count == 0 ? "(none)" : string.Join(" | ", failedRequests))}",
                $"Page content snippet: {pageHtml[..Math.Min(pageHtml.Length, 2000)]}"
            });
            Assert.Fail(diagnostics);
        }

        await ExpectTextAsync(page.Locator("h1").First, "Casey Mac");
        await ExpectTextAsync(page.Locator(".profile-header__title").First, "Senior Software Engineer");
        await ExpectTextAsync(page.Locator(".profile-header__bio").First,
            "Builds resilient .NET platforms, developer tooling, and polished portfolio experiences for technical audiences.");
        await ExpectTextAsync(page.Locator(".profile-header__location").First, "Aotearoa New Zealand");

        var contactLinks = page.Locator(".profile-header__links a");
        Assert.Equal(3, await contactLinks.CountAsync());
        await ExpectTextAsync(contactLinks.Nth(0), "Email");
        await ExpectTextAsync(contactLinks.Nth(1), "GitHub");
        await ExpectTextAsync(contactLinks.Nth(2), "Website");
    }

    private static async Task ExpectTextAsync(ILocator locator, string expectedText)
    {
        var actualText = (await locator.TextContentAsync())?.Trim();
        Assert.Equal(expectedText, actualText);
    }

    private static Task<bool> IsFullyWithinViewportAsync(ILocator locator)
        => locator.EvaluateAsync<bool>(
            "el => { const r = el.getBoundingClientRect(); return r.top >= 0 && r.bottom <= window.innerHeight && r.left >= 0 && r.right <= window.innerWidth; }");

    private static async Task<BuildResult> RunDotnetPublishReleaseAsync(string baseHref = "/CV/")
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"cv-smoke-publish-{Guid.NewGuid():N}");
        var projectPath = GetAssemblyMetadata("CVAppProjectPath");
        var args =
            $"publish \"{projectPath}\" -c Release --nologo -o \"{outputDir}\" -p:ReleaseBaseHref=\"{baseHref}\"";
        var result = await RunDotnetAsync(args, GetAssemblyMetadata("RepositoryRoot"));
        return new BuildResult(result.ExitCode, result.Output, result.Error, outputDir);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunDotnetAsync(string arguments, string workingDirectory)
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

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync());

        return (process.ExitCode, outputTask.Result, errorTask.Result);
    }

    private static string GetAssemblyMetadata(string key)
    {
        return typeof(ReleaseSmokeTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value
               ?? throw new InvalidOperationException(
                   $"Assembly metadata '{key}' not found. Ensure SmokeTests.csproj defines it.");
    }
}

sealed class CvStaticHarness : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly string _hostRoot;

    private CvStaticHarness(WebApplication app, string baseUrl, string hostRoot)
    {
        _app = app;
        BaseUrl = baseUrl;
        _hostRoot = hostRoot;
    }

    public string BaseUrl { get; }

    public static async Task<CvStaticHarness> StartAsync(string publishedWwwroot, string routePrefix = "/CV")
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var hostRoot = Path.Combine(Path.GetTempPath(), $"cv-smoke-host-{Guid.NewGuid():N}");
        var normalizedRoutePrefix = routePrefix.Trim('/');
        var cvRoot = Path.Combine(hostRoot,
            normalizedRoutePrefix.Replace('/', Path.DirectorySeparatorChar));
        DirectoryCopy(publishedWwwroot, cvRoot);

        var app = builder.Build();

        app.MapGet("/", context =>
        {
            context.Response.Redirect($"{routePrefix.TrimEnd('/')}/");
            return Task.CompletedTask;
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(hostRoot),
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream"
        });

        app.MapFallback(async context =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(Path.Combine(cvRoot, "index.html"));
        });

        await app.StartAsync();
        var addressesFeature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var boundAddress = addressesFeature?.Addresses
            .Single(url => url.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Failed to resolve bound local server address.");
        return new CvStaticHarness(app, boundAddress, hostRoot);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        if (Directory.Exists(_hostRoot))
            Directory.Delete(_hostRoot, recursive: true);
    }

    private static void DirectoryCopy(string sourceDir, string destinationDir)
    {
        var source = new DirectoryInfo(sourceDir);
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        Directory.CreateDirectory(destinationDir);

        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(destinationDir, file.Name), overwrite: true);
        }

        foreach (var subDirectory in source.GetDirectories())
        {
            DirectoryCopy(subDirectory.FullName, Path.Combine(destinationDir, subDirectory.Name));
        }
    }
}

sealed record BuildResult(int ExitCode, string Output, string Error, string OutputDirectory) : IDisposable
{
    public bool Succeeded => ExitCode == 0;

    public void Dispose()
    {
        if (Directory.Exists(OutputDirectory))
            Directory.Delete(OutputDirectory, recursive: true);
    }
}
