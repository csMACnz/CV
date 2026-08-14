using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ContentManagement;

/// <summary>
/// Aggregates YAML and Markdown content files from the local content directory
/// into an <see cref="ExperiencePayload"/>. Used by both the build-time Aggregation
/// tool (Release) and the local API endpoint (Debug / local Aspire).
/// </summary>
public static class ContentAggregator
{
    public static ExperiencePayload Aggregate(string contentRoot)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var profilePath = Path.Combine(contentRoot, "profile.yaml");
        if (!File.Exists(profilePath))
            throw new FileNotFoundException("Expected profile content file was not found.", profilePath);

        var profileData = deserializer.Deserialize<ProfileData>(File.ReadAllText(profilePath));
        var profile = new Profile(
            profileData.Name ?? throw new InvalidOperationException("Profile name is required."),
            profileData.Title ?? throw new InvalidOperationException("Profile title is required."),
            profileData.Bio ?? throw new InvalidOperationException("Profile bio is required."),
            profileData.Location ?? throw new InvalidOperationException("Profile location is required."),
            profileData.Links?.Select(link => new ContactLink(
                link.Label ?? throw new InvalidOperationException("Profile link label is required."),
                link.Url ?? throw new InvalidOperationException("Profile link URL is required."),
                link.IconKey ?? string.Empty))
                .ToList() ?? []);

        var employmentRoot = Path.Combine(contentRoot, "employment");
        var timelineEntries = new List<TimelineEntry>();

        if (Directory.Exists(employmentRoot))
        {
            foreach (var employerDir in Directory.GetDirectories(employmentRoot).OrderBy(dir => dir))
            {
                var employerYamlPath = Path.Combine(employerDir, "employer.yaml");
                if (!File.Exists(employerYamlPath))
                    continue;

                var employerData = deserializer.Deserialize<EmployerData>(File.ReadAllText(employerYamlPath));
                var roles = new List<Role>();

                foreach (var roleDir in Directory.GetDirectories(employerDir).OrderBy(dir => dir))
                {
                    var roleYamlPath = Path.Combine(roleDir, "role.yaml");
                    if (!File.Exists(roleYamlPath))
                        continue;

                    var roleData = deserializer.Deserialize<RoleData>(File.ReadAllText(roleYamlPath));
                    var projects = new List<Project>();

                    foreach (var projectDir in Directory.GetDirectories(roleDir).OrderBy(dir => dir))
                    {
                        var projectYamlPath = Path.Combine(projectDir, "project.yaml");
                        var projectMdPath = Path.Combine(projectDir, "project.md");
                        if (!File.Exists(projectYamlPath))
                            continue;

                        var projectData = deserializer.Deserialize<ProjectData>(File.ReadAllText(projectYamlPath));
                        var narrative = File.Exists(projectMdPath)
                            ? File.ReadAllText(projectMdPath).Trim()
                            : string.Empty;

                        projects.Add(new Project(
                            projectData.Name ?? Path.GetFileName(projectDir),
                            projectData.Skills ?? [],
                            narrative));
                    }

                    roles.Add(new Role(
                        roleData.Title ?? Path.GetFileName(roleDir),
                        roleData.Start,
                        roleData.End,
                        projects));
                }

                timelineEntries.Add(new TimelineEntry(
                    employerData.Company ?? Path.GetFileName(employerDir),
                    employerData.Period,
                    employerData.Location,
                    roles));
            }
        }

        return new ExperiencePayload(profile, timelineEntries);
    }
}
