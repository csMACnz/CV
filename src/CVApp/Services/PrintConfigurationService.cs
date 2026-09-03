namespace CVApp.Services;

public enum TimelineScope
{
    FullHistory,
    Last5Years,
    Last10Years
}

public enum ProjectVerbosity
{
    Brief,
    Full
}

public enum SkillLayout
{
    Compact,
    Matrix
}

public sealed record PrintConfiguration(
    TimelineScope TimelineScope = TimelineScope.FullHistory,
    ProjectVerbosity ProjectVerbosity = ProjectVerbosity.Brief,
    SkillLayout SkillLayout = SkillLayout.Compact);

public sealed class PrintConfigurationService
{
    public event Action? Changed;

    public PrintConfiguration Current { get; private set; } = new();

    public void Update(PrintConfiguration configuration)
    {
        Current = configuration;
        Changed?.Invoke();
    }

    public string GetTimelineScopeClass() => Current.TimelineScope switch
    {
        TimelineScope.FullHistory => "print-timeline-full",
        TimelineScope.Last5Years => "print-timeline-5yr",
        TimelineScope.Last10Years => "print-timeline-10yr",
        _ => "print-timeline-full"
    };

    public string GetProjectVerbosityClass() => Current.ProjectVerbosity switch
    {
        ProjectVerbosity.Brief => "print-projects-brief",
        ProjectVerbosity.Full => "print-projects-full",
        _ => "print-projects-brief"
    };

    public string GetSkillLayoutClass() => Current.SkillLayout switch
    {
        SkillLayout.Compact => "print-skills-compact",
        SkillLayout.Matrix => "print-skills-matrix",
        _ => "print-skills-compact"
    };

    public bool IsRoleIncludedInTimeline(Role role, TimelineScope timelineScope, DateOnly referenceDate)
    {
        if (timelineScope == TimelineScope.FullHistory)
            return true;

        if (string.IsNullOrWhiteSpace(role.End))
            return true;

        var cutoff = timelineScope switch
        {
            TimelineScope.Last5Years => referenceDate.AddYears(-5),
            TimelineScope.Last10Years => referenceDate.AddYears(-10),
            _ => DateOnly.MinValue
        };

        if (TimelineDateParser.TryParse(role.End, out var roleEnd))
            return roleEnd >= cutoff;

        if (TimelineDateParser.TryParse(role.Start ?? string.Empty, out var roleStart))
            return roleStart >= cutoff;

        return true;
    }

    public IReadOnlyList<TimelineEntry> FilterTimelineEntries(
        IReadOnlyList<TimelineEntry> entries,
        TimelineScope timelineScope,
        DateOnly referenceDate)
    {
        if (timelineScope == TimelineScope.FullHistory)
            return entries;

        var filtered = new List<TimelineEntry>();
        foreach (var entry in entries)
        {
            var roles = entry.Roles
                .Where(role => IsRoleIncludedInTimeline(role, timelineScope, referenceDate))
                .ToList();

            if (roles.Count > 0)
                filtered.Add(entry with { Roles = roles });
        }

        return filtered;
    }
}
