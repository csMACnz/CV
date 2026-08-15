#if DEBUG
internal sealed record CreateSkillCategoryRequest(string Name);
internal sealed record RenameSkillCategoryRequest(string NewName);
internal sealed record CreateSkillRequest(string CategoryName, string Id, string Name, string? Url);
internal sealed record UpdateSkillRequest(string Name, string? Url);

internal sealed record CreateTimelineEntryRequest(
    string Company,
    string? Period,
    string? Location,
    string RoleTitle,
    string? Start,
    string? End);

internal sealed record UpdateTimelineEntryRequest(
    string Company,
    string? Period,
    string? Location,
    string RoleTitle,
    string? Start,
    string? End);

internal sealed record CreateProjectRequest(
    string Name,
    string? BriefSummary,
    string? Narrative,
    List<string> AppliedSkillIds);

internal sealed record UpdateProjectRequest(
    string Name,
    string? BriefSummary,
    string? Narrative,
    List<string> AppliedSkillIds);
#endif
