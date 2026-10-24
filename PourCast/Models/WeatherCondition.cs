namespace PourCast.Models;

/// <summary>
/// Normalised weather condition shared across all providers.
/// </summary>
public enum WeatherCondition
{
    /// <summary>
    /// The condition could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Clear sky.
    /// </summary>
    Clear,

    /// <summary>
    /// Partly cloudy sky.
    /// </summary>
    PartlyCloudy,

    /// <summary>
    /// Overcast sky.
    /// </summary>
    Cloudy,

    /// <summary>
    /// Fog or mist.
    /// </summary>
    Fog,

    /// <summary>
    /// Light drizzle.
    /// </summary>
    Drizzle,

    /// <summary>
    /// Light or moderate rain.
    /// </summary>
    Rain,

    /// <summary>
    /// Heavy rain.
    /// </summary>
    HeavyRain,

    /// <summary>
    /// Freezing rain or drizzle.
    /// </summary>
    FreezingRain,

    /// <summary>
    /// Rain showers.
    /// </summary>
    Showers,

    /// <summary>
    /// Sleet.
    /// </summary>
    Sleet,

    /// <summary>
    /// Hail.
    /// </summary>
    Hail,

    /// <summary>
    /// Snowfall.
    /// </summary>
    Snow,

    /// <summary>
    /// Snow showers.
    /// </summary>
    SnowShowers,

    /// <summary>
    /// Thunderstorm.
    /// </summary>
    Thunderstorm
}