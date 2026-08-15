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

    // ── Timeline CRUD ────────────────────────────────────────────────────────

    public Task AddTimelineEntryAsync(
        string path,
        ExperiencePayload fallback,
        string company,
        string? period,
        string? location,
        string roleTitle,
        string? start,
        string? end,
        CancellationToken cancellationToken = default)
        => AddTimelineEntryAsync(path, () => fallback, company, period, location, roleTitle, start, end, cancellationToken);

    public Task AddTimelineEntryAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string company,
        string? period,
        string? location,
        string roleTitle,
        string? start,
        string? end,
        CancellationToken cancellationToken = default)
        => MutateTimelineAsync(path, fallbackFactory, timeline =>
        {
            if (string.IsNullOrWhiteSpace(company))
                throw new ArgumentException("Company name is required.", nameof(company));

            if (timeline.Any(entry => string.Equals(entry.Company, company, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Timeline entry for '{company}' already exists.");

            var role = new Role(
                string.IsNullOrWhiteSpace(roleTitle) ? "Role" : roleTitle.Trim(),
                start,
                end,
                []);

            timeline.Add(new TimelineEntry(company.Trim(), period, location, [role]));
        }, cancellationToken);

    public Task UpdateTimelineEntryAsync(
        string path,
        ExperiencePayload fallback,
        string entryId,
        string company,
        string? period,
        string? location,
        string roleTitle,
        string? start,
        string? end,
        CancellationToken cancellationToken = default)
        => UpdateTimelineEntryAsync(path, () => fallback, entryId, company, period, location, roleTitle, start, end, cancellationToken);

    public Task UpdateTimelineEntryAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string entryId,
        string company,
        string? period,
        string? location,
        string roleTitle,
        string? start,
        string? end,
        CancellationToken cancellationToken = default)
        => MutateTimelineAsync(path, fallbackFactory, timeline =>
        {
            var index = timeline.FindIndex(e =>
                string.Equals(e.Company, entryId, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
                throw new KeyNotFoundException($"Timeline entry '{entryId}' was not found.");

            if (!string.Equals(entryId, company, StringComparison.OrdinalIgnoreCase) &&
                timeline.Any(e => string.Equals(e.Company, company, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A timeline entry for '{company}' already exists.");
            }

            var current = timeline[index];
            var existingRoles = current.Roles;

            List<Role> updatedRoles;
            if (existingRoles.Count > 0)
            {
                var firstRole = existingRoles[0];
                var updatedFirst = firstRole with
                {
                    Title = string.IsNullOrWhiteSpace(roleTitle) ? firstRole.Title : roleTitle.Trim(),
                    Start = start,
                    End = end
                };
                updatedRoles = [updatedFirst, .. existingRoles.Skip(1)];
            }
            else
            {
                updatedRoles = [new Role(string.IsNullOrWhiteSpace(roleTitle) ? "Role" : roleTitle.Trim(), start, end, [])];
            }

            timeline[index] = current with
            {
                Company = company.Trim(),
                Period = period,
                Location = location,
                Roles = updatedRoles
            };
        }, cancellationToken);

    public Task DeleteTimelineEntryAsync(
        string path,
        ExperiencePayload fallback,
        string entryId,
        CancellationToken cancellationToken = default)
        => DeleteTimelineEntryAsync(path, () => fallback, entryId, cancellationToken);

    public Task DeleteTimelineEntryAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string entryId,
        CancellationToken cancellationToken = default)
        => MutateTimelineAsync(path, fallbackFactory, timeline =>
        {
            var removed = timeline.RemoveAll(e =>
                string.Equals(e.Company, entryId, StringComparison.OrdinalIgnoreCase));

            if (removed == 0)
                throw new KeyNotFoundException($"Timeline entry '{entryId}' was not found.");
        }, cancellationToken);

    // ── Project CRUD ─────────────────────────────────────────────────────────

    public Task AddProjectAsync(
        string path,
        ExperiencePayload fallback,
        string entryId,
        string name,
        string? briefSummary,
        string? narrative,
        List<string> appliedSkillIds,
        CancellationToken cancellationToken = default)
        => AddProjectAsync(path, () => fallback, entryId, name, briefSummary, narrative, appliedSkillIds, cancellationToken);

    public Task AddProjectAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string entryId,
        string name,
        string? briefSummary,
        string? narrative,
        List<string> appliedSkillIds,
        CancellationToken cancellationToken = default)
        => MutateTimelineAsync(path, fallbackFactory, timeline =>
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Project name is required.", nameof(name));

            var entry = FindTimelineEntry(timeline, entryId);
            if (entry is null)
                throw new KeyNotFoundException($"Timeline entry '{entryId}' was not found.");

            var allProjects = entry.Roles.SelectMany(r => r.Projects);
            if (allProjects.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Project '{name}' already exists in entry '{entryId}'.");

            if (entry.Roles.Count == 0)
                throw new InvalidOperationException($"Timeline entry '{entryId}' has no roles to attach a project to.");

            entry.Roles[0].Projects.Add(new Project(name.Trim(), appliedSkillIds, narrative ?? string.Empty, briefSummary));
        }, cancellationToken);

    public Task UpdateProjectAsync(
        string path,
        ExperiencePayload fallback,
        string entryId,
        string projectId,
        string name,
        string? briefSummary,
        string? narrative,
        List<string> appliedSkillIds,
        CancellationToken cancellationToken = default)
        => UpdateProjectAsync(path, () => fallback, entryId, projectId, name, briefSummary, narrative, appliedSkillIds, cancellationToken);

    public Task UpdateProjectAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string entryId,
        string projectId,
        string name,
        string? briefSummary,
        string? narrative,
        List<string> appliedSkillIds,
        CancellationToken cancellationToken = default)
        => MutateTimelineAsync(path, fallbackFactory, timeline =>
        {
            var located = FindProject(timeline, entryId, projectId);
            if (located is null)
                throw new KeyNotFoundException($"Project '{projectId}' was not found in entry '{entryId}'.");

            if (!string.Equals(projectId, name, StringComparison.OrdinalIgnoreCase))
            {
                var allProjects = FindTimelineEntry(timeline, entryId)!.Roles.SelectMany(r => r.Projects);
                if (allProjects.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Project '{name}' already exists in entry '{entryId}'.");
            }

            located.Role.Projects[located.ProjectIndex] = located.Project with
            {
                Name = name.Trim(),
                BriefSummary = briefSummary,
                Narrative = narrative ?? string.Empty,
                Skills = appliedSkillIds
            };
        }, cancellationToken);

    public Task DeleteProjectAsync(
        string path,
        ExperiencePayload fallback,
        string entryId,
        string projectId,
        CancellationToken cancellationToken = default)
        => DeleteProjectAsync(path, () => fallback, entryId, projectId, cancellationToken);

    public Task DeleteProjectAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        string entryId,
        string projectId,
        CancellationToken cancellationToken = default)
        => MutateTimelineAsync(path, fallbackFactory, timeline =>
        {
            var located = FindProject(timeline, entryId, projectId);
            if (located is null)
                throw new KeyNotFoundException($"Project '{projectId}' was not found in entry '{entryId}'.");

            located.Role.Projects.RemoveAt(located.ProjectIndex);
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

    private static async Task MutateTimelineAsync(
        string path,
        Func<ExperiencePayload> fallbackFactory,
        Action<List<TimelineEntry>> mutate,
        CancellationToken cancellationToken)
        => await MutateRootAsync(path, fallbackFactory, root =>
        {
            var timeline = root["timeline"]?.Deserialize<List<TimelineEntry>>(CamelCaseOptions) ?? [];
            mutate(timeline);
            root["timeline"] = JsonSerializer.SerializeToNode(timeline, CamelCaseOptions);
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

    private static TimelineEntry? FindTimelineEntry(List<TimelineEntry> timeline, string entryId)
        => timeline.FirstOrDefault(e =>
            string.Equals(e.Company, entryId, StringComparison.OrdinalIgnoreCase));

    private static LocatedProject? FindProject(List<TimelineEntry> timeline, string entryId, string projectId)
    {
        var entry = FindTimelineEntry(timeline, entryId);
        if (entry is null)
            return null;

        foreach (var role in entry.Roles)
        {
            var projectIndex = role.Projects.FindIndex(p =>
                string.Equals(p.Name, projectId, StringComparison.OrdinalIgnoreCase));

            if (projectIndex >= 0)
                return new LocatedProject(role, projectIndex, role.Projects[projectIndex]);
        }

        return null;
    }

    private static string? NormalizeUrl(string? url)
        => string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    private sealed record LocatedSkill(SkillGroup Category, int SkillIndex);
    private sealed record LocatedProject(Role Role, int ProjectIndex, Project Project);
}

public record CvDataSourceDocument(Profile Profile, List<TimelineEntry> Timeline, List<SkillGroup> SkillMatrix);
public record SkillGroup(string Name, List<Skill> Skills);
public record Skill(string Id, string Name, string? Url);
