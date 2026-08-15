#if DEBUG
using System.Net;
using System.Net.Http.Json;
using CVApp.Services;

namespace UnitTests;

public class CvAdminStoreServiceTests
{
    [Fact]
    public async Task GetCvDataSourceAsync_ReturnsDeserializedPayload_WhenApiResponds()
    {
        var expected = new CvAdminDataSource(
            new Profile("Test Name", "Test Title", "Test Bio", "Test Location", [
                new ContactLink("Email", "mailto:test@test.com", "email")
            ]),
            [
                new TimelineEntry("Test Co", "2020–2024", "Remote", [
                    new Role("Engineer", "2020-01", "2024-01", [
                        new Project("Test Project", ["C#"], "Did stuff.")
                    ])
                ])
            ],
            [
                new AdminSkillGroup("Languages", [
                    new AdminSkillNode("csharp", "C#", "https://learn.microsoft.com/dotnet/csharp/")
                ])
            ]);

        var json = System.Text.Json.JsonSerializer.Serialize(expected,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        var result = await service.GetCvDataSourceAsync();

        Assert.NotNull(result);
        Assert.Equal("Test Name", result!.Profile.Name);
        Assert.Equal("Test Title", result.Profile.Title);
        Assert.Single(result.Timeline);
        Assert.Equal("Test Co", result.Timeline[0].Company);
        Assert.Single(result.SkillMatrix);
        Assert.Equal("Languages", result.SkillMatrix[0].Name);
    }

    [Fact]
    public async Task GetCvDataSourceAsync_ReturnsNull_WhenApiReturnsNullJson()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null", "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        var result = await service.GetCvDataSourceAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCvDataSourceAsync_ThrowsHttpRequestException_WhenApiReturnsServerError()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, string.Empty, "text/plain");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetCvDataSourceAsync());
    }

    [Fact]
    public async Task GetCvDataSourceAsync_UsesCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null", "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.GetCvDataSourceAsync();

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/cv", handler.LastRequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateProfileAsync_SendsPutRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        var profile = new Profile("New Name", "New Title", "New Bio", "New Location", [
            new ContactLink("GitHub", "https://github.com/test", "github")
        ]);

        await service.UpdateProfileAsync(profile);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/profile", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.LastRequestMethod);
    }

    [Fact]
    public async Task UpdateProfileAsync_ThrowsHttpRequestException_WhenApiReturnsServerError()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, string.Empty, "text/plain");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        var profile = new Profile("Name", "Title", "Bio", "Location", []);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.UpdateProfileAsync(profile));
    }

    [Fact]
    public async Task UpdateProfileAsync_SendsProfileAsJson()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        var profile = new Profile("Jane Doe", "Engineer", "Bio text", "Auckland", [
            new ContactLink("Email", "mailto:jane@example.com", "email")
        ]);

        await service.UpdateProfileAsync(profile);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Jane Doe", handler.LastRequestBody);
        Assert.Contains("Engineer", handler.LastRequestBody);
    }

    [Fact]
    public async Task AddSkillCategoryAsync_SendsPostRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.AddSkillCategoryAsync("Languages");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/skills/categories", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.Contains("Languages", handler.LastRequestBody);
    }

    [Fact]
    public async Task RenameSkillCategoryAsync_SendsPutRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.RenameSkillCategoryAsync("Languages", "Platforms");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/skills/categories/Languages", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.LastRequestMethod);
        Assert.Contains("Platforms", handler.LastRequestBody);
    }

    [Fact]
    public async Task AddSkillAsync_SendsSkillPayloadToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.AddSkillAsync("Languages", new AdminSkillNode("csharp", "C#", "https://learn.microsoft.com/dotnet/csharp/"));

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/skills", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.Contains("Languages", handler.LastRequestBody);
        Assert.Contains("csharp", handler.LastRequestBody);
        Assert.Contains("C#", handler.LastRequestBody);
    }

    [Fact]
    public async Task DeleteSkillCategoryAsync_SendsDeleteRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.DeleteSkillCategoryAsync("Languages");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/skills/categories/Languages", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Delete, handler.LastRequestMethod);
    }

    [Fact]
    public async Task UpdateSkillAsync_SendsPutRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.UpdateSkillAsync("csharp", "C# 13", "https://learn.microsoft.com/dotnet/csharp/");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/skills/csharp", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.LastRequestMethod);
        Assert.Contains("C# 13", handler.LastRequestBody);
    }

    [Fact]
    public async Task DeleteSkillAsync_SendsDeleteRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.DeleteSkillAsync("csharp");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/skills/csharp", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Delete, handler.LastRequestMethod);
    }

    [Fact]
    public async Task AddTimelineEntryAsync_SendsPostRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.AddTimelineEntryAsync("Acme Corp", "2020–2024", "Remote", "Senior Engineer", "2020-01", "2024-06");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/timeline", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.Contains("Acme Corp", handler.LastRequestBody);
        Assert.Contains("Senior Engineer", handler.LastRequestBody);
    }

    [Fact]
    public async Task UpdateTimelineEntryAsync_SendsPutRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.UpdateTimelineEntryAsync("Acme Corp", "New Corp", "2020–2025", "Hybrid", "Lead Engineer", "2020-01", "2025-01");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/timeline/Acme%20Corp", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.LastRequestMethod);
        Assert.Contains("New Corp", handler.LastRequestBody);
        Assert.Contains("Lead Engineer", handler.LastRequestBody);
    }

    [Fact]
    public async Task DeleteTimelineEntryAsync_SendsDeleteRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.DeleteTimelineEntryAsync("Acme Corp");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/timeline/Acme%20Corp", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Delete, handler.LastRequestMethod);
    }

    [Fact]
    public async Task AddProjectAsync_SendsPostRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.AddProjectAsync("Acme Corp", "My Project", "Brief summary", "Detailed narrative", ["csharp"]);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/timeline/Acme%20Corp/projects", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.Contains("My Project", handler.LastRequestBody);
        Assert.Contains("Brief summary", handler.LastRequestBody);
        Assert.Contains("csharp", handler.LastRequestBody);
    }

    [Fact]
    public async Task UpdateProjectAsync_SendsPutRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.UpdateProjectAsync("Acme Corp", "Old Project", "New Project", "New summary", "New narrative", ["csharp", "dotnet"]);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/timeline/Acme%20Corp/projects/Old%20Project", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.LastRequestMethod);
        Assert.Contains("New Project", handler.LastRequestBody);
        Assert.Contains("New summary", handler.LastRequestBody);
    }

    [Fact]
    public async Task DeleteProjectAsync_SendsDeleteRequestToCorrectRoute()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, string.Empty, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new CvAdminStoreService(httpClient);

        await service.DeleteProjectAsync("Acme Corp", "My Project");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/api/admin/timeline/Acme%20Corp/projects/My%20Project", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Delete, handler.LastRequestMethod);
    }

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string content, string mediaType)
        : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public HttpMethod? LastRequestMethod { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestMethod = request.Method;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
            };
            return response;
        }
    }
}
#endif
