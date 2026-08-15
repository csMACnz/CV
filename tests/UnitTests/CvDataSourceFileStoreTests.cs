using System.Text.Json;
using ContentManagement;

namespace UnitTests;

public class CvDataSourceFileStoreTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task AddSkillCategoryAsync_PreservesProfileAndTimelineWhileAppendingCategory()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "Stored Title", bio = "Stored Bio", location = "Stored Location", links = Array.Empty<object>() },
            timeline = new[] { new { company = "Stored Co", period = "2024", location = "Remote", roles = Array.Empty<object>() } },
            skillMatrix = new[] { new { name = "Existing", skills = Array.Empty<object>() } }
        }, CamelCaseOptions));

        await store.AddSkillCategoryAsync(path, fallback, "Languages");
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Name", result.Profile.Name);
        Assert.Single(result.Timeline);
        Assert.Equal("Stored Co", result.Timeline[0].Company);
        Assert.Equal(["Existing", "Languages"], result.SkillMatrix.Select(group => group.Name).ToArray());
    }

    [Fact]
    public async Task UpdateSkillAsync_OnlyChangesTargetSkillWithoutCorruptingOtherRootKeys()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "Stored Title", bio = "Stored Bio", location = "Stored Location", links = Array.Empty<object>() },
            timeline = new[] { new { company = "Stored Co", period = "2024", location = "Remote", roles = Array.Empty<object>() } },
            skillMatrix = new[]
            {
                new
                {
                    name = "Languages",
                    skills = new[]
                    {
                        new { id = "csharp", name = "C#", url = "https://old.example/csharp" },
                        new { id = "dotnet", name = ".NET", url = "https://learn.microsoft.com/dotnet/" }
                    }
                }
            }
        }, CamelCaseOptions));

        await store.UpdateSkillAsync(path, fallback, "csharp", "C# 13", "https://learn.microsoft.com/dotnet/csharp/");
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Title", result.Profile.Title);
        Assert.Equal("Stored Co", result.Timeline[0].Company);
        Assert.Collection(result.SkillMatrix.Single().Skills,
            skill =>
            {
                Assert.Equal("csharp", skill.Id);
                Assert.Equal("C# 13", skill.Name);
                Assert.Equal("https://learn.microsoft.com/dotnet/csharp/", skill.Url);
            },
            skill =>
            {
                Assert.Equal("dotnet", skill.Id);
                Assert.Equal(".NET", skill.Name);
                Assert.Equal("https://learn.microsoft.com/dotnet/", skill.Url);
            });
    }

    [Fact]
    public async Task RenameSkillCategoryAsync_RenamesCategoryWithoutChangingNestedSkills()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "Stored Title", bio = "Stored Bio", location = "Stored Location", links = Array.Empty<object>() },
            timeline = new[] { new { company = "Stored Co", period = "2024", location = "Remote", roles = Array.Empty<object>() } },
            skillMatrix = new[]
            {
                new
                {
                    name = "Languages",
                    skills = new[]
                    {
                        new { id = "csharp", name = "C#", url = "https://learn.microsoft.com/dotnet/csharp/" }
                    }
                }
            }
        }, CamelCaseOptions));

        await store.RenameSkillCategoryAsync(path, fallback, "Languages", "Platforms");
        var result = await store.LoadAsync(path, fallback);

        Assert.Single(result.SkillMatrix);
        Assert.Equal("Platforms", result.SkillMatrix[0].Name);
        Assert.Single(result.SkillMatrix[0].Skills);
        Assert.Equal("csharp", result.SkillMatrix[0].Skills[0].Id);
    }

    [Fact]
    public async Task RenameSkillCategoryAsync_ThrowsWhenNewNameAlreadyExists()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "Stored Title", bio = "Stored Bio", location = "Stored Location", links = Array.Empty<object>() },
            timeline = new[] { new { company = "Stored Co", period = "2024", location = "Remote", roles = Array.Empty<object>() } },
            skillMatrix = new[]
            {
                new { name = "Languages", skills = Array.Empty<object>() },
                new { name = "Platforms", skills = Array.Empty<object>() }
            }
        }, CamelCaseOptions));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.RenameSkillCategoryAsync(path, fallback, "Languages", "Platforms"));
    }

    [Fact]
    public async Task DeleteSkillAsync_RemovesSkillFromCategory()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "Stored Title", bio = "Stored Bio", location = "Stored Location", links = Array.Empty<object>() },
            timeline = new[] { new { company = "Stored Co", period = "2024", location = "Remote", roles = Array.Empty<object>() } },
            skillMatrix = new[]
            {
                new
                {
                    name = "Languages",
                    skills = new[]
                    {
                        new { id = "csharp", name = "C#", url = "https://learn.microsoft.com/dotnet/csharp/" }
                    }
                }
            }
        }, CamelCaseOptions));

        await store.DeleteSkillAsync(path, fallback, "csharp");
        var result = await store.LoadAsync(path, fallback);

        Assert.Single(result.SkillMatrix);
        Assert.Empty(result.SkillMatrix[0].Skills);
        Assert.Equal("Stored Bio", result.Profile.Bio);
    }

    [Fact]
    public async Task DeleteSkillCategoryAsync_ThrowsWhenCategoryDoesNotExist()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "Stored Title", bio = "Stored Bio", location = "Stored Location", links = Array.Empty<object>() },
            timeline = new[] { new { company = "Stored Co", period = "2024", location = "Remote", roles = Array.Empty<object>() } },
            skillMatrix = new[]
            {
                new { name = "Languages", skills = Array.Empty<object>() }
            }
        }, CamelCaseOptions));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            store.DeleteSkillCategoryAsync(path, fallback, "Missing"));
    }

    private static ExperiencePayload CreateFallbackPayload()
        => new(
            new Profile("Fallback Name", "Fallback Title", "Fallback Bio", "Fallback Location", []),
            [new TimelineEntry("Fallback Co", "2020", "Fallback", [])]);

    private static string CreateTempPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cv-admin-store-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "cv-datasource.json");
    }
}
