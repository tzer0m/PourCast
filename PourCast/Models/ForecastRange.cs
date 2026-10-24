namespace PourCast.Models;

/// <summary>
/// The forecast ranges a provider can supply data for.
/// </summary>
public enum ForecastRange
{
    /// <summary>
    /// Single days and the 7 day view.
    /// </summary>
    Week,

    /// <summary>
    /// The 30 day view.
    /// </summary>
    Month
}