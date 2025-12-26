namespace XmlComparer.Core
{
    /// <summary>
    /// Defines a contract for custom value normalization during XML comparison.
    /// </summary>
    /// <remarks>
    /// <para>Value normalizers transform XML text values before comparison to handle
    /// common formatting differences. Implement this interface to create custom normalization logic.</para>
    /// <para>Normalizers are applied during the comparison process when <see cref="XmlDiffConfig.IgnoreValues"/>
    /// is false. They can be registered via <see cref="XmlComparisonOptions.UseValueNormalizer"/> or
    /// added to the <see cref="XmlDiffConfig.ValueNormalizers"/> collection.</para>
    /// <para><b>Common use cases:</b></para>
    /// <list type="bullet">
    ///   <item><description>Date/time normalization (different formats, time zones)</description></item>
    ///   <item><description>Numeric value tolerance (floating-point comparison)</description></item>
    ///   <item><description>Boolean format normalization (true/false, yes/no, 1/0)</description></item>
    ///   <item><description>Custom regex-based transformations</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomDateNormalizer : IValueNormalizer
    /// {
    ///     public string? Normalize(string? value, XmlDiffConfig config)
    ///     {
    ///         if (DateTime.TryParse(value, out var dt))
    ///             return dt.ToString("yyyy-MM-dd HH:mm:ss");
    ///         return value;
    ///     }
    /// }
    ///
    /// // Register the normalizer
    /// options.UseValueNormalizer(new CustomDateNormalizer());
    /// </code>
    /// </example>
    /// <seealso cref="XmlValueNormalizer"/>
    /// <seealso cref="XmlDiffConfig"/>
    public interface IValueNormalizer
    {
        /// <summary>
        /// Normalizes a value string for comparison.
        /// </summary>
        /// <param name="value">The value to normalize, or null if the value is null.</param>
        /// <param name="config">The comparison configuration settings.</param>
        /// <returns>The normalized value, or null if the input was null.</returns>
        /// <remarks>
        /// <para>This method is called for each text value and attribute value during comparison.
        /// Implementations should handle null values gracefully by returning null.</para>
        /// <para>The normalization is applied after the built-in normalizers (whitespace, newlines, trimming)
        /// specified in <see cref="XmlDiffConfig"/>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// public string? Normalize(string? value, XmlDiffConfig config)
        /// {
        ///     if (string.IsNullOrEmpty(value)) return value;
        ///
        ///     // Remove currency symbols for numeric comparison
        ///     return value.Replace("$", "").Replace("€", "").Replace("£", "");
        /// }
        /// </code>
        /// </example>
        string? Normalize(string? value, XmlDiffConfig config);
    }
}
