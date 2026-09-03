using System.Globalization;

namespace CVApp.Services;

public static class TimelineDateParser
{
    private static readonly string[] SupportedDateFormats = ["yyyy-MM", "yyyy-M", "yyyy"];

    public static bool TryParse(string rawValue, out DateOnly parsedDate)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            parsedDate = default;
            return false;
        }

        return DateOnly.TryParseExact(
                   rawValue.Trim(),
                   SupportedDateFormats,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out parsedDate)
               || DateOnly.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
    }
}
