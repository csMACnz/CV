#if DEBUG
internal sealed record CreateSkillCategoryRequest(string Name);
internal sealed record RenameSkillCategoryRequest(string NewName);
internal sealed record CreateSkillRequest(string CategoryName, string Id, string Name, string? Url);
internal sealed record UpdateSkillRequest(string Name, string? Url);
#endif
