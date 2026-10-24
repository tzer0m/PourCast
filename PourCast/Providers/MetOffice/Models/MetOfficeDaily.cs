namespace PourCast.Providers.MetOffice.Models;

/// <summary>
/// A daily time series entry from the Met Office.
/// </summary>
public class MetOfficeDaily
{
    /// <summary>
    /// The UTC date of the day.
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Daytime significant weather code.
    /// </summary>
    public int? DaySignificantWeatherCode { get; set; }

    /// <summary>
    /// Night time significant weather code.
    /// </summary>
    public int? NightSignificantWeatherCode { get; set; }

    /// <summary>
    /// Daytime maximum temperature in °C.
    /// </summary>
    public double? DayMaxScreenTemperature { get; set; }

    /// <summary>
    /// Night time minimum temperature in °C.
    /// </summary>
    public double? NightMinScreenTemperature { get; set; }

    /// <summary>
    /// Daytime maximum feels like temperature in °C.
    /// </summary>
    public double? DayMaxFeelsLikeTemp { get; set; }

    /// <summary>
    /// Night time minimum feels like temperature in °C.
    /// </summary>
    public double? NightMinFeelsLikeTemp { get; set; }

    /// <summary>
    /// Daytime chance of precipitation in percent.
    /// </summary>
    public double? DayProbabilityOfPrecipitation { get; set; }

    /// <summary>
    /// Night time chance of precipitation in percent.
    /// </summary>
    public double? NightProbabilityOfPrecipitation { get; set; }

    /// <summary>
    /// Midday wind speed in m/s.
    /// </summary>
    public double? Midday10MWindSpeed { get; set; }

    /// <summary>
    /// Midnight wind speed in m/s.
    /// </summary>
    public double? Midnight10MWindSpeed { get; set; }

    /// <summary>
    /// Midday wind gust speed in m/s.
    /// </summary>
    public double? Midday10MWindGust { get; set; }

    /// <summary>
    /// Midnight wind gust speed in m/s.
    /// </summary>
    public double? Midnight10MWindGust { get; set; }

    /// <summary>
    /// Maximum UV index.
    /// </summary>
    public double? MaxUvIndex { get; set; }
}