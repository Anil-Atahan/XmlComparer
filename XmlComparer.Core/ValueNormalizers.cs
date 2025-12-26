using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace XmlComparer.Core
{
    /// <summary>
    /// Normalizes date and time values to a standard format for comparison.
    /// </summary>
    /// <remarks>
    /// <para>This normalizer attempts to parse various date/time formats and convert them
    /// to a standardized format, allowing comparison of dates that may be formatted differently.</para>
    /// <para>Supported formats include ISO 8601, RFC 1123, common culture-specific formats, and Unix timestamps.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var normalizer = new DateTimeNormalizer("yyyy-MM-dd HH:mm:ss");
    /// normalizer.Normalize("2024-01-15T10:30:00Z", config); // "2024-01-15 10:30:00"
    /// normalizer.Normalize("Jan 15, 2024", config);           // "2024-01-15 00:00:00"
    /// </code>
    /// </example>
    public class DateTimeNormalizer : IValueNormalizer
    {
        private readonly string _outputFormat;
        private readonly CultureInfo _culture;
        private readonly bool _convertToUtc;

        /// <summary>
        /// Creates a new DateTimeNormalizer with the specified output format.
        /// </summary>
        /// <param name="outputFormat">The .NET format string for output (default: "yyyy-MM-dd HH:mm:ss").</param>
        /// <param name="culture">The culture to use for parsing (default: invariant culture).</param>
        /// <param name="convertToUtc">Whether to convert all dates to UTC (default: false).</param>
        public DateTimeNormalizer(string outputFormat = "yyyy-MM-dd HH:mm:ss", CultureInfo? culture = null, bool convertToUtc = false)
        {
            _outputFormat = outputFormat ?? "yyyy-MM-dd HH:mm:ss";
            _culture = culture ?? CultureInfo.InvariantCulture;
            _convertToUtc = convertToUtc;
        }

        /// <summary>
        /// Normalizes a date/time value to the standard format.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <param name="config">The comparison configuration (unused).</param>
        /// <returns>The normalized date/time string, or the original value if parsing fails.</returns>
        public string? Normalize(string? value, XmlDiffConfig config)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            // Try Unix timestamp (seconds since epoch)
            if (long.TryParse(value.Trim(), out long unixSeconds))
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return (_convertToUtc ? dt.UtcDateTime : dt.DateTime).ToString(_outputFormat, _culture);
            }

            // Try ISO 8601 with timezone
            if (DateTimeOffset.TryParse(value, _culture, DateTimeStyles.None, out var dto))
            {
                return (_convertToUtc ? dto.UtcDateTime : dto.DateTime).ToString(_outputFormat, _culture);
            }

            // Try standard DateTime
            if (DateTime.TryParse(value, _culture, DateTimeStyles.None, out var dateTime))
            {
                return dateTime.ToString(_outputFormat, _culture);
            }

            // Try common formats
            string[] formats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyyMMdd", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(value, formats, _culture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate.ToString(_outputFormat, _culture);
            }

            return value; // Return original if not a recognized date format
        }
    }

    /// <summary>
    /// Normalizes numeric values with tolerance for floating-point comparison.
    /// </summary>
    /// <remarks>
    /// <para>This normalizer handles numeric comparisons with configurable tolerance,
    /// useful for comparing floating-point numbers that may have minor precision differences.</para>
    /// <para>Values are rounded to the specified number of decimal places before comparison.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var normalizer = new NumericNormalizer(3); // 3 decimal places
    /// normalizer.Normalize("3.14159265", config); // "3.142"
    /// normalizer.Normalize("3.1415000", config); // "3.142"
    /// </code>
    /// </example>
    public class NumericNormalizer : IValueNormalizer
    {
        private readonly int _decimalPlaces;

        /// <summary>
        /// Creates a new NumericNormalizer with the specified precision.
        /// </summary>
        /// <param name="decimalPlaces">The number of decimal places to round to (default: 6).</param>
        public NumericNormalizer(int decimalPlaces = 6)
        {
            _decimalPlaces = decimalPlaces;
        }

        /// <summary>
        /// Normalizes a numeric value by rounding to the specified precision.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <param name="config">The comparison configuration (unused).</param>
        /// <returns>The rounded numeric string, or the original value if not a number.</returns>
        public string? Normalize(string? value, XmlDiffConfig config)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            // Try parsing as double
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
            {
                // Round to specified decimal places and format invariantly
                double rounded = Math.Round(num, _decimalPlaces);
                // Remove trailing zeros after decimal point
                string formatted = rounded.ToString("0." + new string('0', _decimalPlaces), CultureInfo.InvariantCulture);
                return formatted.Replace(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator, ".");
            }

            return value;
        }
    }

    /// <summary>
    /// Normalizes boolean values to a standard format.
    /// </summary>
    /// <remarks>
    /// <para>This normalizer recognizes various boolean representations and converts them
    /// to a standard "true" or "false" format.</para>
    /// <para>Recognized true values: true, True, TRUE, 1, yes, Yes, YES, on, On, ON</para>
    /// <para>Recognized false values: false, False, FALSE, 0, no, No, NO, off, Off, OFF</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var normalizer = new BooleanNormalizer();
    /// normalizer.Normalize("yes", config);    // "true"
    /// normalizer.Normalize("0", config);     // "false"
    /// normalizer.Normalize("TRUE", config);  // "true"
    /// </code>
    /// </example>
    public class BooleanNormalizer : IValueNormalizer
    {
        private readonly string _trueString;
        private readonly string _falseString;

        /// <summary>
        /// Creates a new BooleanNormalizer with customizable output strings.
        /// </summary>
        /// <param name="trueString">The string to use for true values (default: "true").</param>
        /// <param name="falseString">The string to use for false values (default: "false").</param>
        public BooleanNormalizer(string trueString = "true", string falseString = "false")
        {
            _trueString = trueString ?? "true";
            _falseString = falseString ?? "false";
        }

        /// <summary>
        /// Normalizes a boolean value to the standard format.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <param name="config">The comparison configuration (unused).</param>
        /// <returns>The normalized boolean string, or the original value if not a recognized boolean.</returns>
        public string? Normalize(string? value, XmlDiffConfig config)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            string normalized = value.Trim().ToLowerInvariant();

            // Recognized true values
            if (normalized is "true" or "1" or "yes" or "on")
                return _trueString;

            // Recognized false values
            if (normalized is "false" or "0" or "no" or "off")
                return _falseString;

            return value; // Return original if not a recognized boolean
        }
    }

    /// <summary>
    /// Normalizes values using regular expression replacement.
    /// </summary>
    /// <remarks>
    /// <para>This normalizer applies regex pattern/replacement pairs to transform values,
    /// useful for removing currency symbols, formatting characters, or other patterns.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var normalizer = new RegexNormalizer();
    /// // Remove currency symbols
    /// normalizer.AddPattern(@"[$€£]", "");
    /// // Remove commas from numbers
    /// normalizer.AddPattern(@",(?=\d{3})", "");
    /// </code>
    /// </example>
    public class RegexNormalizer : IValueNormalizer
    {
        private readonly List<(Regex Pattern, string Replacement)> _patterns = new();

        /// <summary>
        /// Creates a new RegexNormalizer.
        /// </summary>
        public RegexNormalizer() { }

        /// <summary>
        /// Creates a new RegexNormalizer with predefined patterns.
        /// </summary>
        /// <param name="patterns">Collection of (pattern, replacement) tuples.</param>
        public RegexNormalizer(IEnumerable<(string pattern, string replacement)> patterns)
        {
            foreach (var (pattern, replacement) in patterns)
            {
                AddPattern(pattern, replacement);
            }
        }

        /// <summary>
        /// Adds a regex pattern and replacement string.
        /// </summary>
        /// <param name="pattern">The regex pattern to match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="options">Regex options (default: IgnoreCase).</param>
        public void AddPattern(string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase)
        {
            _patterns.Add((new Regex(pattern, options), replacement));
        }

        /// <summary>
        /// Normalizes a value by applying all registered regex patterns in sequence.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <param name="config">The comparison configuration (unused).</param>
        /// <returns>The normalized value, or the original value if no patterns are registered.</returns>
        public string? Normalize(string? value, XmlDiffConfig config)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            string result = value;
            foreach (var (pattern, replacement) in _patterns)
            {
                result = pattern.Replace(result, replacement);
            }
            return result;
        }
    }
}
