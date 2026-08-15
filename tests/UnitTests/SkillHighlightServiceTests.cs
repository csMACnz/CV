using CVApp.Services;

namespace UnitTests;

/// <summary>
/// Unit tests for <see cref="SkillHighlightService"/>.
/// Verifies that the highlight-mapping service correctly returns the intersection
/// of a Skill ID and the Timeline experience entries.
/// </summary>
public class SkillHighlightServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Project MakeProject(string name, params string[] skills) =>
        new(name, skills.ToList(), string.Empty);

    private static Role MakeRole(string title, params Project[] projects) =>
        new(title, null, null, projects.ToList());

    private static TimelineEntry MakeEntry(string company, params Role[] roles) =>
        new(company, null, null, roles.ToList());

    // ── GetAllSkills ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllSkills_ReturnsDistinctSortedSkills()
    {
        var timeline = new List<TimelineEntry>
        {
            MakeEntry("Acme",
                MakeRole("Dev", MakeProject("P1", "C#", "Azure"), MakeProject("P2", "Azure", "Docker"))),
            MakeEntry("Beta",
                MakeRole("Lead", MakeProject("P3", "c#", "Kubernetes")))
        };

        var svc = new SkillHighlightService();
        var skills = svc.GetAllSkills(timeline);

        // Distinct (case-insensitive) and sorted alphabetically
        Assert.Equal(new[] { "Azure", "C#", "Docker", "Kubernetes" }, skills);
    }

    [Fact]
    public void GetAllSkills_EmptyTimeline_ReturnsEmptyList()
    {
        var svc = new SkillHighlightService();
        var skills = svc.GetAllSkills([]);
        Assert.Empty(skills);
    }

    // ── BuildHighlightMap – null / empty skill ──────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildHighlightMap_NullOrWhitespaceSkill_ReturnsEmptyMap(string? skillId)
    {
        var timeline = new List<TimelineEntry>
        {
            MakeEntry("Acme", MakeRole("Dev", MakeProject("P1", "C#")))
        };

        var svc = new SkillHighlightService();
        var map = svc.BuildHighlightMap(timeline, skillId);

        Assert.False(map.IsActive);
    }

    // ── BuildHighlightMap – matching entries ────────────────────────────────────

    [Fact]
    public void BuildHighlightMap_MatchingSkill_HighlightsCorrectProjectRoleAndEntry()
    {
        var projectA = MakeProject("Alpha", "C#", "Azure");
        var projectB = MakeProject("Beta", "Docker");
        var roleA    = MakeRole("Dev", projectA, projectB);
        var entryA   = MakeEntry("Acme", roleA);

        var projectC = MakeProject("Gamma", "C#");
        var roleB    = MakeRole("Lead", projectC);
        var entryB   = MakeEntry("Corp", roleB);

        var timeline = new List<TimelineEntry> { entryA, entryB };

        var svc = new SkillHighlightService();
        var map = svc.BuildHighlightMap(timeline, "C#");

        Assert.True(map.IsActive);

        // Both entries contain C#
        Assert.True(map.IsEntryHighlighted(entryA));
        Assert.True(map.IsEntryHighlighted(entryB));

        // Correct roles
        Assert.True(map.IsRoleHighlighted(roleA));
        Assert.True(map.IsRoleHighlighted(roleB));

        // Only projects with C# are highlighted
        Assert.True(map.IsProjectHighlighted(projectA));
        Assert.False(map.IsProjectHighlighted(projectB)); // only has Docker
        Assert.True(map.IsProjectHighlighted(projectC));
    }

    [Fact]
    public void BuildHighlightMap_SkillNotPresent_ReturnsInactiveMap()
    {
        var timeline = new List<TimelineEntry>
        {
            MakeEntry("Acme", MakeRole("Dev", MakeProject("P1", "C#", "Azure")))
        };

        var svc = new SkillHighlightService();
        var map = svc.BuildHighlightMap(timeline, "Kubernetes");

        Assert.False(map.IsActive);
    }

    [Fact]
    public void BuildHighlightMap_IsCaseInsensitive()
    {
        var project = MakeProject("P1", "c#");
        var role    = MakeRole("Dev", project);
        var entry   = MakeEntry("Acme", role);

        var svc = new SkillHighlightService();
        var map = svc.BuildHighlightMap([entry], "C#");

        Assert.True(map.IsActive);
        Assert.True(map.IsProjectHighlighted(project));
    }

    [Fact]
    public void BuildHighlightMap_EntryWithNoMatchingRoles_IsNotHighlighted()
    {
        var projectMatching    = MakeProject("Match", "C#");
        var projectNonMatching = MakeProject("NoMatch", "Docker");
        var roleMatching       = MakeRole("DevRole", projectMatching);
        var roleNonMatching    = MakeRole("OpsRole", projectNonMatching);
        var entry              = MakeEntry("Acme", roleMatching, roleNonMatching);

        var svc = new SkillHighlightService();
        var map = svc.BuildHighlightMap([entry], "C#");

        Assert.True(map.IsEntryHighlighted(entry));
        Assert.True(map.IsRoleHighlighted(roleMatching));
        Assert.False(map.IsRoleHighlighted(roleNonMatching));
    }

    // ── HighlightMap.Empty ──────────────────────────────────────────────────────

    [Fact]
    public void HighlightMap_Empty_IsNotActive()
    {
        Assert.False(HighlightMap.Empty.IsActive);
    }
}
