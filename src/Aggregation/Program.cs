using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Aggregation <contentRoot> <outputFile>");
    return 1;
}

var contentRoot = args[0];
var outputFile = args[1];

if (!Directory.Exists(contentRoot))
{
    Console.Error.WriteLine($"Content directory not found: {contentRoot}");
    return 1;
}

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
                var narrative = File.Exists(projectMdPath) ? File.ReadAllText(projectMdPath).Trim() : string.Empty;

                projects.Add(new Project(
                    projectData.Name ?? Path.GetFileName(projectDir),
                    projectData.Skills ?? new List<string>(),
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

var payload = new ExperiencePayload(timelineEntries);

var outputDir = Path.GetDirectoryName(outputFile);
if (!string.IsNullOrEmpty(outputDir))
    Directory.CreateDirectory(outputDir);

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};
File.WriteAllText(outputFile, JsonSerializer.Serialize(payload, options));
Console.WriteLine($"Aggregated experience data written to: {outputFile}");
return 0;

record EmployerData
{
    public string? Company { get; init; }
    public string? Period { get; init; }
    public string? Location { get; init; }
}

record RoleData
{
    public string? Title { get; init; }
    public string? Start { get; init; }
    public string? End { get; init; }
}

record ProjectData
{
    public string? Name { get; init; }
    public List<string>? Skills { get; init; }
}

record Project(string Name, List<string> Skills, string Narrative);
record Role(string Title, string? Start, string? End, List<Project> Projects);
record TimelineEntry(string Company, string? Period, string? Location, List<Role> Roles);
record ExperiencePayload(List<TimelineEntry> Timeline);
