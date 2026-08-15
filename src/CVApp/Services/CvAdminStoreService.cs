#if DEBUG
using System.Net.Http.Json;

namespace CVApp.Services;

/// <summary>
/// Seam interface for the local Admin CMS data store.
/// Registered exclusively in Debug builds (ADR-004 / ADR-011).
/// </summary>
public interface ICvAdminStoreService
{
    /// <summary>
    /// Fetches the full aggregated CV data graph from the local Admin API endpoint.
    /// </summary>
    Task<CvAdminDataSource?> GetCvDataSourceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the given <paramref name="profile"/> to the local Admin API endpoint
    /// via <c>PUT /api/admin/profile</c>.
    /// </summary>
    Task UpdateProfileAsync(Profile profile, CancellationToken cancellationToken = default);

    Task AddSkillCategoryAsync(string categoryName, CancellationToken cancellationToken = default);
    Task RenameSkillCategoryAsync(string categoryName, string newCategoryName, CancellationToken cancellationToken = default);
    Task DeleteSkillCategoryAsync(string categoryName, CancellationToken cancellationToken = default);
    Task AddSkillAsync(string categoryName, SkillNode skill, CancellationToken cancellationToken = default);
    Task UpdateSkillAsync(string skillId, string name, string? url, CancellationToken cancellationToken = default);
    Task DeleteSkillAsync(string skillId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fetches the aggregate CV data graph from the local .NET Aspire backend
/// over <c>GET /api/admin/cv</c>. Only compiled and registered in Debug mode.
/// </summary>
public class CvAdminStoreService : ICvAdminStoreService
{
    private readonly HttpClient _httpClient;

    public CvAdminStoreService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public Task<CvAdminDataSource?> GetCvDataSourceAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<CvAdminDataSource>("/api/admin/cv", cancellationToken);

    /// <inheritdoc />
    public async Task UpdateProfileAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/admin/profile", profile, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task AddSkillCategoryAsync(string categoryName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/admin/skills/categories",
            new CreateSkillCategoryRequest(categoryName),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task RenameSkillCategoryAsync(string categoryName, string newCategoryName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"/api/admin/skills/categories/{Uri.EscapeDataString(categoryName)}",
            new RenameSkillCategoryRequest(newCategoryName),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task DeleteSkillCategoryAsync(string categoryName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"/api/admin/skills/categories/{Uri.EscapeDataString(categoryName)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task AddSkillAsync(string categoryName, SkillNode skill, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/admin/skills",
            new CreateSkillRequest(categoryName, skill.Id, skill.Name, skill.Url),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task UpdateSkillAsync(string skillId, string name, string? url, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"/api/admin/skills/{Uri.EscapeDataString(skillId)}",
            new UpdateSkillRequest(name, url),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task DeleteSkillAsync(string skillId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"/api/admin/skills/{Uri.EscapeDataString(skillId)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public record CvAdminDataSource(Profile Profile, IReadOnlyList<TimelineEntry> Timeline, IReadOnlyList<SkillGroup> SkillMatrix);
public record SkillGroup(string Name, IReadOnlyList<SkillNode> Skills);
public record SkillNode(string Id, string Name, string? Url);

internal sealed record CreateSkillCategoryRequest(string Name);
internal sealed record RenameSkillCategoryRequest(string NewName);
internal sealed record CreateSkillRequest(string CategoryName, string Id, string Name, string? Url);
internal sealed record UpdateSkillRequest(string Name, string? Url);
#endif
