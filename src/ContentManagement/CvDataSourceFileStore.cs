using System.Text.Json;
using System.Text.Json.Nodes;

namespace ContentManagement;

public sealed class CvDataSourceFileStore
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions IndentedCamelCaseOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<CvDataSourceDocument> LoadAsync(
        string path,
        ExperiencePayload fallback,
        CancellationToken cancellationToken = default)
        => await LoadAsync(path, () => fallback, cancellationToken);

    public async Task<CvDataSourceDocument> LoadAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(path, fallbackFactory, cancellationToken);

        var profile = root["profile"]?.Deserialize<Profile>(CamelCaseOptions)
            ?? throw new InvalidOperationException("CV data source is missing a profile payload.");
        var timeline = root["timeline"]?.Deserialize<List<TimelineEntry>>(CamelCaseOptions)
            ?? throw new InvalidOperationException("CV data source is missing a timeline payload.");
        var skillMatrix = root["skillMatrix"]?.Deserialize<List<SkillGroup>>(CamelCaseOptions)
            ?? [];

        return new CvDataSourceDocument(profile, timeline, skillMatrix);
    }

    public Task UpdateProfileAsync(
        string path,
        ExperiencePayload fallback,
        Profile profile,
        CancellationToken cancellationToken = default)
        => UpdateProfileAsync(path, () => fallback, profile, cancellationToken);

    public Task UpdateProfileAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        Profile profile,
        CancellationToken cancellationToken = default)
        => MutateRootAsync(path, fallbackFactory, root =>
        {
            root["profile"] = JsonSerializer.SerializeToNode(profile, CamelCaseOptions);
        }, cancellationToken);

    public Task AddSkillCategoryAsync(
        string path,
        ExperiencePayload fallback,
        string categoryName,
        CancellationToken cancellationToken = default)
        => AddSkillCategoryAsync(path, () => fallback, categoryName, cancellationToken);

    public Task AddSkillCategoryAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string categoryName,
        CancellationToken cancellationToken = default)
        => MutateSkillMatrixAsync(path, fallbackFactory, skillMatrix =>
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name is required.", nameof(categoryName));

            if (skillMatrix.Any(group => string.Equals(group.Name, categoryName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category '{categoryName}' already exists.");

            skillMatrix.Add(new SkillGroup(categoryName.Trim(), []));
        }, cancellationToken);

    public Task RenameSkillCategoryAsync(
        string path,
        ExperiencePayload fallback,
        string categoryName,
        string newCategoryName,
        CancellationToken cancellationToken = default)
        => RenameSkillCategoryAsync(path, () => fallback, categoryName, newCategoryName, cancellationToken);

    public Task RenameSkillCategoryAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string categoryName,
        string newCategoryName,
        CancellationToken cancellationToken = default)
        => MutateSkillMatrixAsync(path, fallbackFactory, skillMatrix =>
        {
            if (string.IsNullOrWhiteSpace(newCategoryName))
                throw new ArgumentException("New category name is required.", nameof(newCategoryName));

            var categoryIndex = skillMatrix.FindIndex(group =>
                string.Equals(group.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (categoryIndex < 0)
                throw new KeyNotFoundException($"Category '{categoryName}' was not found.");

            if (skillMatrix
                .Where((_, index) => index != categoryIndex)
                .Any(group => string.Equals(group.Name, newCategoryName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Category '{newCategoryName}' already exists.");
            }

            var current = skillMatrix[categoryIndex];
            skillMatrix[categoryIndex] = current with { Name = newCategoryName.Trim() };
        }, cancellationToken);

    public Task DeleteSkillCategoryAsync(
        string path,
        ExperiencePayload fallback,
        string categoryName,
        CancellationToken cancellationToken = default)
        => DeleteSkillCategoryAsync(path, () => fallback, categoryName, cancellationToken);

    public Task DeleteSkillCategoryAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string categoryName,
        CancellationToken cancellationToken = default)
        => MutateSkillMatrixAsync(path, fallbackFactory, skillMatrix =>
        {
            var removedCount = skillMatrix.RemoveAll(group =>
                string.Equals(group.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (removedCount == 0)
                throw new KeyNotFoundException($"Category '{categoryName}' was not found.");
        }, cancellationToken);

    public Task AddSkillAsync(
        string path,
        ExperiencePayload fallback,
        string categoryName,
        Skill skill,
        CancellationToken cancellationToken = default)
        => AddSkillAsync(path, () => fallback, categoryName, skill, cancellationToken);

    public Task AddSkillAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string categoryName,
        Skill skill,
        CancellationToken cancellationToken = default)
        => MutateSkillMatrixAsync(path, fallbackFactory, skillMatrix =>
        {
            if (string.IsNullOrWhiteSpace(skill.Id))
                throw new ArgumentException("Skill ID is required.", nameof(skill));

            if (string.IsNullOrWhiteSpace(skill.Name))
                throw new ArgumentException("Skill name is required.", nameof(skill));

            var categoryIndex = skillMatrix.FindIndex(group =>
                string.Equals(group.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (categoryIndex < 0)
                throw new KeyNotFoundException($"Category '{categoryName}' was not found.");

            if (skillMatrix.SelectMany(group => group.Skills).Any(existing =>
                    string.Equals(existing.Id, skill.Id, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Skill '{skill.Id}' already exists.");
            }

            var category = skillMatrix[categoryIndex];
            category.Skills.Add(new Skill(skill.Id.Trim(), skill.Name.Trim(), NormalizeUrl(skill.Url)));
        }, cancellationToken);

    public Task UpdateSkillAsync(
        string path,
        ExperiencePayload fallback,
        string skillId,
        string name,
        string? url,
        CancellationToken cancellationToken = default)
        => UpdateSkillAsync(path, () => fallback, skillId, name, url, cancellationToken);

    public Task UpdateSkillAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string skillId,
        string name,
        string? url,
        CancellationToken cancellationToken = default)
        => MutateSkillMatrixAsync(path, fallbackFactory, skillMatrix =>
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Skill name is required.", nameof(name));

            var located = FindSkill(skillMatrix, skillId);
            if (located is null)
                throw new KeyNotFoundException($"Skill '{skillId}' was not found.");

            var current = located.Category.Skills[located.SkillIndex];
            located.Category.Skills[located.SkillIndex] = current with
            {
                Name = name.Trim(),
                Url = NormalizeUrl(url)
            };
        }, cancellationToken);

    public Task DeleteSkillAsync(
        string path,
        ExperiencePayload fallback,
        string skillId,
        CancellationToken cancellationToken = default)
        => DeleteSkillAsync(path, () => fallback, skillId, cancellationToken);

    public Task DeleteSkillAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string skillId,
        CancellationToken cancellationToken = default)
        => MutateSkillMatrixAsync(path, fallbackFactory, skillMatrix =>
        {
            var located = FindSkill(skillMatrix, skillId);
            if (located is null)
                throw new KeyNotFoundException($"Skill '{skillId}' was not found.");

            located.Category.Skills.RemoveAt(located.SkillIndex);
        }, cancellationToken);

    private static async Task MutateSkillMatrixAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        Action<List<SkillGroup>> mutate,
        CancellationToken cancellationToken)
        => await MutateRootAsync(path, fallbackFactory, root =>
        {
            var skillMatrix = root["skillMatrix"]?.Deserialize<List<SkillGroup>>(CamelCaseOptions) ?? [];
            mutate(skillMatrix);
            root["skillMatrix"] = JsonSerializer.SerializeToNode(skillMatrix, CamelCaseOptions);
        }, cancellationToken);

    private static async Task MutateRootAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        Action<JsonObject> mutate,
        CancellationToken cancellationToken)
    {
        var root = await LoadRootAsync(path, fallbackFactory, cancellationToken);
        mutate(root);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, root.ToJsonString(IndentedCamelCaseOptions), cancellationToken);
    }

    private static async Task<JsonObject> LoadRootAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        CancellationToken cancellationToken)
    {
        JsonObject root;
        try
        {
            var existing = await File.ReadAllTextAsync(path, cancellationToken);
            root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            root = new JsonObject();
        }

        var fallback = new Lazy<ExperiencePayload>(fallbackFactory);

        root["profile"] ??= JsonSerializer.SerializeToNode(fallback.Value.Profile, CamelCaseOptions);
        root["timeline"] ??= JsonSerializer.SerializeToNode(fallback.Value.Timeline, CamelCaseOptions);
        root["skillMatrix"] ??= JsonSerializer.SerializeToNode(new List<SkillGroup>(), CamelCaseOptions);

        return root;
    }

    private static LocatedSkill? FindSkill(List<SkillGroup> skillMatrix, string skillId)
    {
        foreach (var category in skillMatrix)
        {
            var skillIndex = category.Skills.FindIndex(skill =>
                string.Equals(skill.Id, skillId, StringComparison.OrdinalIgnoreCase));

            if (skillIndex >= 0)
                return new LocatedSkill(category, skillIndex);
        }

        return null;
    }

    private static string? NormalizeUrl(string? url)
        => string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    private sealed record LocatedSkill(SkillGroup Category, int SkillIndex);
}

public record CvDataSourceDocument(Profile Profile, List<TimelineEntry> Timeline, List<SkillGroup> SkillMatrix);
public record SkillGroup(string Name, List<Skill> Skills);
public record Skill(string Id, string Name, string? Url);
