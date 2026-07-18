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
        return _httpClient.GetFromJsonAsync<ExperiencePayload>("/api/experience", cancellationToken);
#else
        // In Release (production) mode, data is fetched from the compiled static payload.
        return _httpClient.GetFromJsonAsync<ExperiencePayload>("/data/experience.json", cancellationToken);
#endif
    }
}

public record ExperiencePayload(List<TimelineEntry> Timeline);
public record TimelineEntry(string Company, string? Period, string? Location, List<Role> Roles);
public record Role(string Title, string? Start, string? End, List<Project> Projects);
public record Project(string Name, List<string> Skills, string Narrative);
