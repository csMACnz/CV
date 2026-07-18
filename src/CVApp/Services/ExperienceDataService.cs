using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CVApp.Services;

public class ExperienceDataService
{
    private readonly HttpClient _httpClient;

    public ExperienceDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ExperiencePayload?> GetExperienceAsync(CancellationToken cancellationToken = default)
    {
#if DEBUG
        // In Debug (local Aspire) mode, data is sourced from the local API endpoint.
        // Relative URL resolves against HttpClient.BaseAddress (HostEnvironment.BaseAddress),
        // so subpath-hosted deployments (e.g. GitHub Pages) are handled correctly.
        return _httpClient.GetFromJsonAsync<ExperiencePayload>("/experience", cancellationToken);
#else
        // In Release (production) mode, data is fetched from the compiled static payload.
        // Relative URL resolves against HttpClient.BaseAddress (HostEnvironment.BaseAddress),
        // so subpath-hosted deployments (e.g. GitHub Pages) are handled correctly.
        return _httpClient.GetFromJsonAsync<ExperiencePayload>("data/experience.json", cancellationToken);
#endif
    }
}

public record ExperiencePayload(IReadOnlyList<TimelineEntry> Timeline);
public record TimelineEntry(string Company, string? Period, string? Location, IReadOnlyList<Role> Roles);
public record Role(string Title, string? Start, string? End, IReadOnlyList<Project> Projects);
public record Project(string Name, IReadOnlyList<string> Skills, string Narrative);
