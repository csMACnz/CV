namespace CVApp.Services;

/// <summary>
/// Service layer responsible for mapping a skill identifier to the timeline entries
/// (employers, roles, and projects) that reference that skill.
/// Keeps highlight-mapping logic out of UI components.
/// </summary>
public class SkillHighlightService
{
    /// <summary>
    /// Returns the set of all distinct skill names found across the given timeline.
    /// </summary>
    public IReadOnlyList<string> GetAllSkills(IReadOnlyList<TimelineEntry> timeline)
    {
        return timeline
            .SelectMany(e => e.Roles)
            .SelectMany(r => r.Projects)
            .SelectMany(p => p.Skills)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns a <see cref="HighlightMap"/> that records which timeline entries, roles,
    /// and projects are associated with the given <paramref name="skillId"/>.
    /// Returns an empty map when <paramref name="skillId"/> is null or whitespace.
    /// </summary>
    public HighlightMap BuildHighlightMap(IReadOnlyList<TimelineEntry> timeline, string? skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return HighlightMap.Empty;

        var highlightedEntries = new HashSet<TimelineEntry>();
        var highlightedRoles   = new HashSet<Role>();
        var highlightedProjects = new HashSet<Project>();

        foreach (var entry in timeline)
        {
            foreach (var role in entry.Roles)
            {
                foreach (var project in role.Projects)
                {
                    if (project.Skills.Any(s => string.Equals(s, skillId, StringComparison.OrdinalIgnoreCase)))
                    {
                        highlightedProjects.Add(project);
                        highlightedRoles.Add(role);
                        highlightedEntries.Add(entry);
                    }
                }
            }
        }

        return new HighlightMap(highlightedEntries, highlightedRoles, highlightedProjects);
    }
}

/// <summary>
/// Immutable snapshot of which timeline nodes are highlighted for a given active skill.
/// </summary>
public sealed class HighlightMap
{
    public static readonly HighlightMap Empty = new([], [], []);

    private readonly IReadOnlySet<TimelineEntry> _entries;
    private readonly IReadOnlySet<Role> _roles;
    private readonly IReadOnlySet<Project> _projects;

    public HighlightMap(
        IEnumerable<TimelineEntry> entries,
        IEnumerable<Role> roles,
        IEnumerable<Project> projects)
    {
        _entries  = entries as IReadOnlySet<TimelineEntry>  ?? new HashSet<TimelineEntry>(entries);
        _roles    = roles   as IReadOnlySet<Role>           ?? new HashSet<Role>(roles);
        _projects = projects as IReadOnlySet<Project>       ?? new HashSet<Project>(projects);
    }

    public bool IsActive => _entries.Count > 0;

    public bool IsEntryHighlighted(TimelineEntry entry)   => _entries.Contains(entry);
    public bool IsRoleHighlighted(Role role)               => _roles.Contains(role);
    public bool IsProjectHighlighted(Project project)      => _projects.Contains(project);
}
