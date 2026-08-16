namespace ContentManagement;

/// <summary>
/// Domain models for the experience payload served by both the build-time
/// Aggregation tool (Release) and the local API endpoint (Debug).
/// </summary>
public record ExperiencePayload(Profile Profile, List<TimelineEntry> Timeline);
public record Profile(string Name, string Title, string Bio, string Location, List<ContactLink> Links);
public record ContactLink(string Label, string Url, string IconKey);
public record TimelineEntry(string Company, string? Period, string? Location, List<Role> Roles);
public record Role(string Title, string? Start, string? End, List<Project> Projects);
public record Project(string Name, List<string> Skills, string Narrative, string? BriefSummary = null);

internal record ProfileData
{
    public string? Name { get; init; }
    public string? Title { get; init; }
    public string? Bio { get; init; }
    public string? Location { get; init; }
    public List<ContactLinkData>? Links { get; init; }
}

internal record ContactLinkData
{
    public string? Label { get; init; }
    public string? Url { get; init; }
    public string? IconKey { get; init; }
}

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
    public string? BriefSummary { get; init; }
}
