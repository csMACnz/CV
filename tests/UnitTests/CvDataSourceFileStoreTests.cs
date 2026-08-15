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

    // ── Timeline CRUD tests ───────────────────────────────────────────────────

    [Fact]
    public async Task AddTimelineEntryAsync_AppendsEntryWhilePreservingProfileAndSkillMatrix()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "Stored Title", bio = "Stored Bio", location = "Stored Location", links = Array.Empty<object>() },
            timeline = Array.Empty<object>(),
            skillMatrix = new[] { new { name = "Languages", skills = Array.Empty<object>() } }
        }, CamelCaseOptions));

        await store.AddTimelineEntryAsync(path, fallback, "Acme Corp", "2020–2024", "Remote", "Senior Engineer", "2020-01", "2024-06");
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Name", result.Profile.Name);
        Assert.Single(result.SkillMatrix);
        Assert.Single(result.Timeline);
        Assert.Equal("Acme Corp", result.Timeline[0].Company);
        Assert.Equal("2020–2024", result.Timeline[0].Period);
        Assert.Equal("Remote", result.Timeline[0].Location);
        Assert.Single(result.Timeline[0].Roles);
        Assert.Equal("Senior Engineer", result.Timeline[0].Roles[0].Title);
        Assert.Equal("2020-01", result.Timeline[0].Roles[0].Start);
        Assert.Equal("2024-06", result.Timeline[0].Roles[0].End);
    }

    [Fact]
    public async Task AddTimelineEntryAsync_ThrowsWhenCompanyAlreadyExists()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = new[] { new { company = "Acme Corp", period = "2024", location = "Remote", roles = Array.Empty<object>() } },
            skillMatrix = Array.Empty<object>()
        }, CamelCaseOptions));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.AddTimelineEntryAsync(path, fallback, "Acme Corp", null, null, "Engineer", null, null));
    }

    [Fact]
    public async Task UpdateTimelineEntryAsync_UpdatesEntryFieldsAndPreservesOtherRootKeys()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = new[]
            {
                new { company = "Old Corp", period = "2020–2023", location = "Remote", roles = new[] { new { title = "Dev", start = "2020-01", end = "2023-06", projects = Array.Empty<object>() } } }
            },
            skillMatrix = new[] { new { name = "Languages", skills = Array.Empty<object>() } }
        }, CamelCaseOptions));

        await store.UpdateTimelineEntryAsync(path, fallback, "Old Corp", "New Corp", "2020–2024", "Hybrid", "Senior Dev", "2020-01", "2024-12");
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Name", result.Profile.Name);
        Assert.Single(result.SkillMatrix);
        Assert.Single(result.Timeline);
        Assert.Equal("New Corp", result.Timeline[0].Company);
        Assert.Equal("2020–2024", result.Timeline[0].Period);
        Assert.Equal("Hybrid", result.Timeline[0].Location);
        Assert.Equal("Senior Dev", result.Timeline[0].Roles[0].Title);
        Assert.Equal("2024-12", result.Timeline[0].Roles[0].End);
    }

    [Fact]
    public async Task UpdateTimelineEntryAsync_ThrowsWhenEntryNotFound()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "N", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = Array.Empty<object>(),
            skillMatrix = Array.Empty<object>()
        }, CamelCaseOptions));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            store.UpdateTimelineEntryAsync(path, fallback, "Missing Corp", "New Corp", null, null, "Dev", null, null));
    }

    [Fact]
    public async Task DeleteTimelineEntryAsync_RemovesEntryAndPreservesOtherRootKeys()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = new[]
            {
                new { company = "Keep Corp", period = "2020", location = "Remote", roles = Array.Empty<object>() },
                new { company = "Remove Corp", period = "2021", location = "Remote", roles = Array.Empty<object>() }
            },
            skillMatrix = new[] { new { name = "Languages", skills = Array.Empty<object>() } }
        }, CamelCaseOptions));

        await store.DeleteTimelineEntryAsync(path, fallback, "Remove Corp");
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Name", result.Profile.Name);
        Assert.Single(result.SkillMatrix);
        Assert.Single(result.Timeline);
        Assert.Equal("Keep Corp", result.Timeline[0].Company);
    }

    [Fact]
    public async Task DeleteTimelineEntryAsync_ThrowsWhenEntryNotFound()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "N", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = Array.Empty<object>(),
            skillMatrix = Array.Empty<object>()
        }, CamelCaseOptions));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            store.DeleteTimelineEntryAsync(path, fallback, "Missing Corp"));
    }

    // ── Project CRUD tests ────────────────────────────────────────────────────

    [Fact]
    public async Task AddProjectAsync_AddsProjectToEntryAndPreservesOtherRootKeys()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = new[]
            {
                new { company = "Acme Corp", period = "2020", location = "Remote", roles = new[] { new { title = "Dev", start = "2020-01", end = "2024-06", projects = Array.Empty<object>() } } }
            },
            skillMatrix = new[] { new { name = "Languages", skills = Array.Empty<object>() } }
        }, CamelCaseOptions));

        await store.AddProjectAsync(path, fallback, "Acme Corp", "My Project", "A brief summary", "Detailed narrative text", ["csharp", "dotnet"]);
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Name", result.Profile.Name);
        Assert.Single(result.SkillMatrix);
        Assert.Single(result.Timeline);
        var projects = result.Timeline[0].Roles[0].Projects;
        Assert.Single(projects);
        Assert.Equal("My Project", projects[0].Name);
        Assert.Equal("A brief summary", projects[0].BriefSummary);
        Assert.Equal("Detailed narrative text", projects[0].Narrative);
        Assert.Equal(["csharp", "dotnet"], projects[0].Skills);
    }

    [Fact]
    public async Task AddProjectAsync_ThrowsWhenEntryNotFound()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "N", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = Array.Empty<object>(),
            skillMatrix = Array.Empty<object>()
        }, CamelCaseOptions));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            store.AddProjectAsync(path, fallback, "Missing Corp", "Project", null, null, []));
    }

    [Fact]
    public async Task UpdateProjectAsync_UpdatesProjectFieldsWithoutCorruptingOtherKeys()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = new[]
            {
                new { company = "Acme Corp", period = "2020", location = "Remote", roles = new[]
                {
                    new { title = "Dev", start = "2020-01", end = "2024-06", projects = new[]
                    {
                        new { name = "Old Project", briefSummary = "Old summary", narrative = "Old narrative", skills = new[] { "csharp" } }
                    }}
                }}
            },
            skillMatrix = new[] { new { name = "Languages", skills = Array.Empty<object>() } }
        }, CamelCaseOptions));

        await store.UpdateProjectAsync(path, fallback, "Acme Corp", "Old Project", "New Project", "New summary", "New narrative", ["csharp", "dotnet"]);
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Name", result.Profile.Name);
        var projects = result.Timeline[0].Roles[0].Projects;
        Assert.Single(projects);
        Assert.Equal("New Project", projects[0].Name);
        Assert.Equal("New summary", projects[0].BriefSummary);
        Assert.Equal("New narrative", projects[0].Narrative);
        Assert.Equal(["csharp", "dotnet"], projects[0].Skills);
    }

    [Fact]
    public async Task DeleteProjectAsync_RemovesProjectAndPreservesOtherRootKeys()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "Stored Name", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = new[]
            {
                new { company = "Acme Corp", period = "2020", location = "Remote", roles = new[]
                {
                    new { title = "Dev", start = "2020-01", end = "2024-06", projects = new[]
                    {
                        new { name = "Keep Project", narrative = "Keep", skills = Array.Empty<string>() },
                        new { name = "Remove Project", narrative = "Remove", skills = Array.Empty<string>() }
                    }}
                }}
            },
            skillMatrix = Array.Empty<object>()
        }, CamelCaseOptions));

        await store.DeleteProjectAsync(path, fallback, "Acme Corp", "Remove Project");
        var result = await store.LoadAsync(path, fallback);

        Assert.Equal("Stored Name", result.Profile.Name);
        var projects = result.Timeline[0].Roles[0].Projects;
        Assert.Single(projects);
        Assert.Equal("Keep Project", projects[0].Name);
    }

    [Fact]
    public async Task DeleteProjectAsync_ThrowsWhenProjectNotFound()
    {
        var store = new CvDataSourceFileStore();
        var fallback = CreateFallbackPayload();
        var path = CreateTempPath();

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            profile = new { name = "N", title = "T", bio = "B", location = "L", links = Array.Empty<object>() },
            timeline = new[]
            {
                new { company = "Acme Corp", period = "2020", location = "Remote", roles = new[] { new { title = "Dev", start = (string?)null, end = (string?)null, projects = Array.Empty<object>() } } }
            },
            skillMatrix = Array.Empty<object>()
        }, CamelCaseOptions));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            store.DeleteProjectAsync(path, fallback, "Acme Corp", "Missing Project"));
    }

    private static string CreateTempPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cv-admin-store-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "cv-datasource.json");
    }
}
