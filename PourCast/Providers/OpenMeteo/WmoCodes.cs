using PourCast.Models;

namespace PourCast.Providers.OpenMeteo;

/// <summary>
/// Converts WMO weather codes, as used by Open-Meteo, to normalised conditions.
/// </summary>
public static class WmoCodes
{
    /// <summary>
    /// Maps a WMO weather code to a normalised condition.
    /// </summary>
    /// <param name="code">The WMO weather code.</param>
    public static WeatherCondition ToCondition(int? code)
    {
        return code switch
        {
            0 => WeatherCondition.Clear,
            1 or 2 => WeatherCondition.PartlyCloudy,
            3 => WeatherCondition.Cloudy,
            45 or 48 => WeatherCondition.Fog,
            51 or 53 or 55 => WeatherCondition.Drizzle,
            56 or 57 or 66 or 67 => WeatherCondition.FreezingRain,
            61 or 63 => WeatherCondition.Rain,
            65 => WeatherCondition.HeavyRain,
            71 or 73 or 75 or 77 => WeatherCondition.Snow,
            80 or 81 or 82 => WeatherCondition.Showers,
            85 or 86 => WeatherCondition.SnowShowers,
            95 or 96 or 99 => WeatherCondition.Thunderstorm,
            _ => WeatherCondition.Unknown
        };
    }
}