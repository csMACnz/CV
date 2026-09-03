using CVApp.Services;

namespace UnitTests;

public class TimelineYearMarkerBuilderTests
{
    [Fact]
    public void Build_ReturnsEmpty_WhenNoDatesArePresent()
    {
        var entries = new[]
        {
            new TimelineEntry("Acme", null, null, [new Role("Engineer", null, null, [])])
        };

        var markers = TimelineYearMarkerBuilder.Build(entries, new DateOnly(2026, 1, 1));

        Assert.Empty(markers);
    }

    [Fact]
    public void Build_ReturnsAllYearsAcrossDatasetRange()
    {
        var entries = new[]
        {
            new TimelineEntry("Acme", "2018–2020", "Remote", [new Role("Engineer", "2018-09", "2020-05", [])]),
            new TimelineEntry("Future", "2023–Present", "Remote", [new Role("Lead", "2023-10", null, [])])
        };

        var markers = TimelineYearMarkerBuilder.Build(entries, new DateOnly(2026, 1, 1));

        Assert.Equal([2018, 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026], markers.Select(marker => marker.Year));
        Assert.Equal("0%", markers[0].Position);
        Assert.Equal("100%", markers[^1].Position);
    }

    [Fact]
    public void Build_UsesSingleCenteredMarker_WhenRangeIsOneYear()
    {
        var entries = new[]
        {
            new TimelineEntry("Acme", "2024", "Remote", [new Role("Engineer", "2024-01", "2024-12", [])])
        };

        var markers = TimelineYearMarkerBuilder.Build(entries, new DateOnly(2026, 1, 1));

        var marker = Assert.Single(markers);
        Assert.Equal(2024, marker.Year);
        Assert.Equal("50%", marker.Position);
    }

    [Fact]
    public void Build_DoesNotTreatInvalidEndDateAsPresent()
    {
        var entries = new[]
        {
            new TimelineEntry("Acme", "2024", "Remote", [new Role("Engineer", "2024-01", "invalid", [])])
        };

        var markers = TimelineYearMarkerBuilder.Build(entries, new DateOnly(2026, 1, 1));

        var marker = Assert.Single(markers);
        Assert.Equal(2024, marker.Year);
        Assert.Equal("50%", marker.Position);
    }

    [Fact]
    public void Build_UsesReferenceYearForPresentPeriodWithoutRoleStart()
    {
        var entries = new[]
        {
            new TimelineEntry("Future", "2023–Present", "Remote", [new Role("Lead", null, null, [])])
        };

        var markers = TimelineYearMarkerBuilder.Build(entries, new DateOnly(2026, 1, 1));

        var marker = Assert.Single(markers);
        Assert.Equal(2026, marker.Year);
        Assert.Equal("50%", marker.Position);
    }

    [Fact]
    public void Build_DoesNotExtendMissingEndDateWithoutPresentPeriod()
    {
        var entries = new[]
        {
            new TimelineEntry("Acme", "2024", "Remote", [new Role("Engineer", "2024-01", null, [])])
        };

        var markers = TimelineYearMarkerBuilder.Build(entries, new DateOnly(2026, 1, 1));

        var marker = Assert.Single(markers);
        Assert.Equal(2024, marker.Year);
        Assert.Equal("50%", marker.Position);
    }
}
