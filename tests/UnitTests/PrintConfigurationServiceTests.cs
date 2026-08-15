using CVApp.Services;

namespace UnitTests;

public class PrintConfigurationServiceTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var service = new PrintConfigurationService();

        Assert.Equal(TimelineScope.FullHistory, service.Current.TimelineScope);
        Assert.Equal(ProjectVerbosity.Brief, service.Current.ProjectVerbosity);
        Assert.Equal(SkillLayout.Compact, service.Current.SkillLayout);
        Assert.Equal("print-timeline-full", service.GetTimelineScopeClass());
        Assert.Equal("print-projects-brief", service.GetProjectVerbosityClass());
        Assert.Equal("print-skills-compact", service.GetSkillLayoutClass());
    }

    [Fact]
    public void Update_ChangesCurrentConfigurationAndRaisesChanged()
    {
        var service = new PrintConfigurationService();
        var changedRaised = false;
        service.Changed += () => changedRaised = true;

        service.Update(new PrintConfiguration(TimelineScope.Last10Years, ProjectVerbosity.Full, SkillLayout.Matrix));

        Assert.True(changedRaised);
        Assert.Equal(TimelineScope.Last10Years, service.Current.TimelineScope);
        Assert.Equal(ProjectVerbosity.Full, service.Current.ProjectVerbosity);
        Assert.Equal(SkillLayout.Matrix, service.Current.SkillLayout);
        Assert.Equal("print-timeline-10yr", service.GetTimelineScopeClass());
        Assert.Equal("print-projects-full", service.GetProjectVerbosityClass());
        Assert.Equal("print-skills-matrix", service.GetSkillLayoutClass());
    }

    [Fact]
    public void IsRoleIncludedInTimeline_FullHistory_AlwaysTrue()
    {
        var service = new PrintConfigurationService();
        var role = new Role("Senior Engineer", "2010-01", "2012-06", []);

        var included = service.IsRoleIncludedInTimeline(role, TimelineScope.FullHistory, new DateOnly(2026, 1, 1));

        Assert.True(included);
    }

    [Fact]
    public void IsRoleIncludedInTimeline_Last5Years_UsesEndDateWindow()
    {
        var service = new PrintConfigurationService();
        var olderRole = new Role("Engineer", "2018-01", "2019-12", []);
        var recentRole = new Role("Lead", "2022-01", "2024-03", []);
        var referenceDate = new DateOnly(2026, 1, 1);

        Assert.False(service.IsRoleIncludedInTimeline(olderRole, TimelineScope.Last5Years, referenceDate));
        Assert.True(service.IsRoleIncludedInTimeline(recentRole, TimelineScope.Last5Years, referenceDate));
    }

    [Fact]
    public void IsRoleIncludedInTimeline_Last10Years_UsesEndDateWindow()
    {
        var service = new PrintConfigurationService();
        var olderRole = new Role("Engineer", "2010-01", "2014-12", []);
        var recentRole = new Role("Lead", "2022-01", "2024-03", []);
        var referenceDate = new DateOnly(2026, 1, 1);

        Assert.False(service.IsRoleIncludedInTimeline(olderRole, TimelineScope.Last10Years, referenceDate));
        Assert.True(service.IsRoleIncludedInTimeline(recentRole, TimelineScope.Last10Years, referenceDate));
    }

    [Fact]
    public void FilterTimelineEntries_RemovesEntriesWithoutAnyMatchingRole()
    {
        var service = new PrintConfigurationService();
        var oldRole = new Role("Old", "2010-01", "2014-12", []);
        var recentRole = new Role("Recent", "2022-01", "2024-03", []);

        var entries = new List<TimelineEntry>
        {
            new("OldCo", "2010–2014", "Remote", [oldRole]),
            new("NewCo", "2022–2024", "Remote", [recentRole]),
        };

        var filtered = service.FilterTimelineEntries(entries, TimelineScope.Last5Years, new DateOnly(2026, 1, 1));

        var filteredList = Assert.IsAssignableFrom<IReadOnlyList<TimelineEntry>>(filtered);
        Assert.Single(filteredList);
        Assert.Equal("NewCo", filteredList[0].Company);
        Assert.Single(filteredList[0].Roles);
        Assert.Equal("Recent", filteredList[0].Roles[0].Title);
    }
}
