namespace PourCast.Providers.MetOffice.Models;

/// <summary>
/// An hourly time series entry from the Met Office.
/// </summary>
public class MetOfficeHourly
{
    /// <summary>
    /// The UTC time of the hour.
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Air temperature in °C.
    /// </summary>
    public double? ScreenTemperature { get; set; }

    /// <summary>
    /// Feels like temperature in °C.
    /// </summary>
    public double? FeelsLikeTemperature { get; set; }

    /// <summary>
    /// Dew point in °C.
    /// </summary>
    public double? ScreenDewPointTemperature { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    public double? ScreenRelativeHumidity { get; set; }

    /// <summary>
    /// Wind speed in m/s.
    /// </summary>
    public double? WindSpeed10m { get; set; }

    /// <summary>
    /// Wind direction in degrees.
    /// </summary>
    public double? WindDirectionFrom10m { get; set; }

    /// <summary>
    /// Wind gust speed in m/s.
    /// </summary>
    public double? WindGustSpeed10m { get; set; }

    /// <summary>
    /// Visibility in metres.
    /// </summary>
    public double? Visibility { get; set; }

    /// <summary>
    /// Mean sea level pressure in Pa.
    /// </summary>
    public double? Mslp { get; set; }

    /// <summary>
    /// UV index.
    /// </summary>
    public double? UvIndex { get; set; }

    /// <summary>
    /// Met Office significant weather code.
    /// </summary>
    public int? SignificantWeatherCode { get; set; }

    /// <summary>
    /// Precipitation rate in mm/h.
    /// </summary>
    public double? PrecipitationRate { get; set; }

    /// <summary>
    /// Total precipitation in mm.
    /// </summary>
    public double? TotalPrecipAmount { get; set; }

    /// <summary>
    /// Total snowfall in mm.
    /// </summary>
    public double? TotalSnowAmount { get; set; }

    /// <summary>
    /// Chance of precipitation in percent.
    /// </summary>
    public double? ProbOfPrecipitation { get; set; }
}