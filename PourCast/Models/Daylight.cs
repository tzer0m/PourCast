namespace PourCast.Models;

/// <summary>
/// Sunrise and sunset for a single day at a location.
/// </summary>
public class Daylight
{
    /// <summary>
    /// The UTC date the times apply to.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Time of sunrise, or null if the sun does not rise.
    /// </summary>
    public DateTimeOffset? Sunrise { get; set; }

    /// <summary>
    /// Time of sunset, or null if the sun does not set.
    /// </summary>
    public DateTimeOffset? Sunset { get; set; }

    /// <summary>
    /// Whether the sun stays up all day (polar day).
    /// </summary>
    public bool AlwaysUp { get; set; }
}