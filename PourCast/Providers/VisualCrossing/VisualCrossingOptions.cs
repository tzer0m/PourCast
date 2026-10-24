namespace PourCast.Providers.VisualCrossing;

/// <summary>
/// Configuration for the Visual Crossing provider.
/// </summary>
public class VisualCrossingOptions : ProviderOptions
{
    /// <summary>
    /// The Visual Crossing API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The number of days to request. Visual Crossing charges one record per hour returned, so fewer days means more calls per day.
    /// </summary>
    public int ForecastDays { get; set; } = 2;
}