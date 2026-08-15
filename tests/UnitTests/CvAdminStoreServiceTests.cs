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
        var expected = new ExperiencePayload(
            new Profile("Test Name", "Test Title", "Test Bio", "Test Location", [
                new ContactLink("Email", "mailto:test@test.com", "email")
            ]),
            [
                new TimelineEntry("Test Co", "2020–2024", "Remote", [
                    new Role("Engineer", "2020-01", "2024-01", [
                        new Project("Test Project", ["C#"], "Did stuff.")
                    ])
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

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string content, string mediaType)
        : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
            };
            return Task.FromResult(response);
        }
    }
}
#endif
