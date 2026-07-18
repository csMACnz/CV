#if DEBUG
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
#endif

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
// ADR-004 / ADR-008: Local CMS API is available only in Debug (local Aspire) builds.
// Allow any origin so the Blazor WASM client can call this API from its own dev-server port.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
#endif

var app = builder.Build();

#if DEBUG
app.UseCors();

// ADR-008 / ADR-010: Serve aggregated experience data from the local content directory.
// ContentRoot config key lets the Aspire AppHost override the default path.
app.MapGet("api/experience", (IConfiguration config, IWebHostEnvironment env) =>
{
    var contentRoot = config["ContentRoot"]
        ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "content"));

    var payload = ContentAggregator.Aggregate(contentRoot);
    return Results.Ok(payload);
});
#endif

app.Run();

#if DEBUG
internal static class ContentAggregator
{
    public static ExperiencePayload Aggregate(string contentRoot)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var employmentRoot = Path.Combine(contentRoot, "employment");
        var timelineEntries = new List<TimelineEntry>();

        if (Directory.Exists(employmentRoot))
        {
            foreach (var employerDir in Directory.GetDirectories(employmentRoot).OrderBy(d => d))
            {
                var employerYamlPath = Path.Combine(employerDir, "employer.yaml");
                if (!File.Exists(employerYamlPath))
                    continue;

                var employerData = deserializer.Deserialize<EmployerData>(File.ReadAllText(employerYamlPath));
                var roles = new List<Role>();

                foreach (var roleDir in Directory.GetDirectories(employerDir).OrderBy(d => d))
                {
                    var roleYamlPath = Path.Combine(roleDir, "role.yaml");
                    if (!File.Exists(roleYamlPath))
                        continue;

                    var roleData = deserializer.Deserialize<RoleData>(File.ReadAllText(roleYamlPath));
                    var projects = new List<Project>();

                    foreach (var projectDir in Directory.GetDirectories(roleDir).OrderBy(d => d))
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

        return new ExperiencePayload(timelineEntries);
    }
}

internal record EmployerData
{
    public string? Company { get; init; }
    public string? Period { get; init; }
    public string? Location { get; init; }
}

internal record RoleData
{
    public string? Title { get; init; }
    public string? Start { get; init; }
    public string? End { get; init; }
}

internal record ProjectData
{
    public string? Name { get; init; }
    public List<string>? Skills { get; init; }
}

internal record Project(string Name, List<string> Skills, string Narrative);
internal record Role(string Title, string? Start, string? End, List<Project> Projects);
internal record TimelineEntry(string Company, string? Period, string? Location, List<Role> Roles);
internal record ExperiencePayload(List<TimelineEntry> Timeline);
#endif
