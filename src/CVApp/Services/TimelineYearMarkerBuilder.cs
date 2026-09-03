namespace CVApp.Services;

public static class TimelineYearMarkerBuilder
{
    public static IReadOnlyList<TimelineYearMarker> Build(IReadOnlyList<TimelineEntry> entries, DateOnly referenceDate)
    {
        var datedRoles = entries
            .SelectMany(entry => entry.Roles)
            .Select(role =>
            {
                var startYear = TryResolveYear(role.Start, fallbackYear: null);
                int? endFallback = startYear.HasValue ? referenceDate.Year : null;

                return new
                {
                    Start = startYear,
                    End = TryResolveYear(role.End, fallbackYear: endFallback)
                };
            })
            .Where(role => role.Start.HasValue || role.End.HasValue)
            .ToList();

        if (datedRoles.Count == 0)
            return [];

        var startYear = datedRoles.Min(role => role.Start ?? role.End!.Value);
        var endYear = datedRoles.Max(role => role.End ?? role.Start!.Value);

        if (startYear == endYear)
            return [new TimelineYearMarker(startYear, "50%")];

        var totalYears = endYear - startYear;
        var markers = new List<TimelineYearMarker>(totalYears + 1);
        for (var year = startYear; year <= endYear; year++)
        {
            var position = (double)(year - startYear) / totalYears * 100d;
            markers.Add(new TimelineYearMarker(year, $"{position:0.##}%"));
        }

        return markers;
    }

    private static int? TryResolveYear(string? rawValue, int? fallbackYear)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return fallbackYear;

        return TimelineDateParser.TryParse(rawValue, out var parsedDate)
            ? parsedDate.Year
            : fallbackYear;
    }
}

public sealed record TimelineYearMarker(int Year, string Position);
