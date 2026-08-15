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
    {
        var root = await LoadRootAsync(path, fallback, cancellationToken);

        var profile = root["profile"]?.Deserialize<Profile>(CamelCaseOptions)
            ?? fallback.Profile;
        var timeline = root["timeline"]?.Deserialize<List<TimelineEntry>>(CamelCaseOptions)
            ?? fallback.Timeline;
        var skillMatrix = root["skillMatrix"]?.Deserialize<List<SkillGroup>>(CamelCaseOptions)
            ?? [];

        return new CvDataSourceDocument(profile, timeline, skillMatrix);
    }

    public Task UpdateProfileAsync(
        string path,
        ExperiencePayload fallback,
        Profile profile,
        CancellationToken cancellationToken = default)
        => MutateRootAsync(path, fallback, root =>
        {
            root["profile"] = JsonSerializer.SerializeToNode(profile, CamelCaseOptions);
        }, cancellationToken);

    public Task AddSkillCategoryAsync(
        string path,
        ExperiencePayload fallback,
        string categoryName,
        CancellationToken cancellationToken = default)
        => MutateSkillMatrixAsync(path, fallback, skillMatrix =>
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
        => MutateSkillMatrixAsync(path, fallback, skillMatrix =>
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
        => MutateSkillMatrixAsync(path, fallback, skillMatrix =>
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
        => MutateSkillMatrixAsync(path, fallback, skillMatrix =>
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
        => MutateSkillMatrixAsync(path, fallback, skillMatrix =>
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
        => MutateSkillMatrixAsync(path, fallback, skillMatrix =>
        {
            var located = FindSkill(skillMatrix, skillId);
            if (located is null)
                throw new KeyNotFoundException($"Skill '{skillId}' was not found.");

            located.Category.Skills.RemoveAt(located.SkillIndex);
        }, cancellationToken);

    private static async Task MutateSkillMatrixAsync(
        string path,
        ExperiencePayload fallback,
        Action<List<SkillGroup>> mutate,
        CancellationToken cancellationToken)
        => await MutateRootAsync(path, fallback, root =>
        {
            var skillMatrix = root["skillMatrix"]?.Deserialize<List<SkillGroup>>(CamelCaseOptions) ?? [];
            mutate(skillMatrix);
            root["skillMatrix"] = JsonSerializer.SerializeToNode(skillMatrix, CamelCaseOptions);
        }, cancellationToken);

    private static async Task MutateRootAsync(
        string path,
        ExperiencePayload fallback,
        Action<JsonObject> mutate,
        CancellationToken cancellationToken)
    {
        var root = await LoadRootAsync(path, fallback, cancellationToken);
        mutate(root);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, root.ToJsonString(IndentedCamelCaseOptions), cancellationToken);
    }

    private static async Task<JsonObject> LoadRootAsync(
        string path,
        ExperiencePayload fallback,
        CancellationToken cancellationToken)
    {
        JsonObject root;
        try
        {
            var existing = await File.ReadAllTextAsync(path, cancellationToken);
            root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
        }
        catch (FileNotFoundException)
        {
            root = new JsonObject();
        }

        root["profile"] ??= JsonSerializer.SerializeToNode(fallback.Profile, CamelCaseOptions);
        root["timeline"] ??= JsonSerializer.SerializeToNode(fallback.Timeline, CamelCaseOptions);
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
