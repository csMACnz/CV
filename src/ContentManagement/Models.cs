namespace ContentManagement;

/// <summary>
/// Domain models for the experience payload served by both the build-time
/// Aggregation tool (Release) and the local API endpoint (Debug).
/// </summary>
public record ExperiencePayload(List<TimelineEntry> Timeline);
public record TimelineEntry(string Company, string? Period, string? Location, List<Role> Roles);
public record Role(string Title, string? Start, string? End, List<Project> Projects);
public record Project(string Name, List<string> Skills, string Narrative);

/// <summary>
/// YAML data transfer objects used when deserialising the raw content files.
/// </summary>
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
