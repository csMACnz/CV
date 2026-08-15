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
    Task<ExperiencePayload?> GetCvDataSourceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the given <paramref name="profile"/> to the local Admin API endpoint
    /// via <c>PUT /api/admin/profile</c>.
    /// </summary>
    Task UpdateProfileAsync(Profile profile, CancellationToken cancellationToken = default);
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
    public Task<ExperiencePayload?> GetCvDataSourceAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<ExperiencePayload>("/api/admin/cv", cancellationToken);

    /// <inheritdoc />
    public async Task UpdateProfileAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/admin/profile", profile, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
#endif
