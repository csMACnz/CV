using System.Globalization;

namespace CVApp.Services;

public static class TimelineDateParser
{
    private static readonly string[] SupportedDateFormats = ["yyyy-MM-dd", "yyyy-M-d", "yyyy-MM", "yyyy-M", "yyyy"];

    public static bool TryParse(string rawValue, out DateOnly parsedDate)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            parsedDate = default;
            return false;
        }

        var trimmedValue = rawValue.Trim();

        return DateOnly.TryParseExact(
            trimmedValue,
            SupportedDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsedDate);
    }
}
