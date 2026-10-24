namespace PourCast.Services;

/// <summary>
/// Calculates feels like temperature the same way for every source, as providers each use different definitions.
/// Uses the JAG/TI wind chill formula when cool, the NWS heat index when hot, and air temperature in between, with smooth blends at each edge.
/// </summary>
public static class FeelsLikeCalculator
{
    /// <summary>
    /// Calculates the feels like temperature, or null if the temperature is missing.
    /// </summary>
    /// <param name="temperature">Air temperature in °C.</param>
    /// <param name="windSpeed">Wind speed in mph.</param>
    /// <param name="humidity">Relative humidity in percent.</param>
    /// <param name="options">The blend thresholds.</param>
    public static double? Calculate(double? temperature, double? windSpeed, double? humidity, CombiningOptions options)
    {
        if (temperature is null)
        {
            return null;
        }
        double t = temperature.Value;

        // Wind chill applies fully at or below the lower threshold and fades out by the upper one.
        if (t < options.WindChillFadeTo && windSpeed is not null)
        {
            double weight = Math.Clamp((options.WindChillFadeTo - t) / (options.WindChillFadeTo - options.WindChillFullAt), 0, 1);
            return t + weight * (WindChill(t, windSpeed.Value) - t);
        }

        // Heat index fades in from the lower threshold and applies fully at the upper one.
        if (t > options.HeatIndexFadeFrom && humidity is not null)
        {
            double weight = Math.Clamp((t - options.HeatIndexFadeFrom) / (options.HeatIndexFullAt - options.HeatIndexFadeFrom), 0, 1);
            return t + weight * (HeatIndex(t, humidity.Value) - t);
        }
        return t;
    }

    /// <summary>
    /// JAG/TI wind chill, used by the Met Office, Environment Canada and the US NWS. Never warmer than the air temperature.
    /// </summary>
    /// <param name="temperature">Air temperature in °C.</param>
    /// <param name="windSpeed">Wind speed in mph.</param>
    private static double WindChill(double temperature, double windSpeed)
    {
        double kilometresPerHour = windSpeed * 1.609344;
        if (kilometresPerHour < 4.8)
        {
            return temperature;
        }
        double power = Math.Pow(kilometresPerHour, 0.16);
        return Math.Min(temperature, 13.12 + 0.6215 * temperature - 11.37 * power + 0.3965 * temperature * power);
    }

    /// <summary>
    /// NWS heat index (Rothfusz regression, with the simple formula for milder conditions). Never cooler than the air temperature.
    /// </summary>
    /// <param name="temperature">Air temperature in °C.</param>
    /// <param name="humidity">Relative humidity in percent.</param>
    private static double HeatIndex(double temperature, double humidity)
    {
        double f = temperature * 9 / 5 + 32;
        double simple = 0.5 * (f + 61 + (f - 68) * 1.2 + humidity * 0.094);
        double index = (simple + f) / 2 < 80 ? simple : -42.379 + 2.04901523 * f + 10.14333127 * humidity - 0.22475541 * f * humidity - 0.00683783 * f * f - 0.05481717 * humidity * humidity + 0.00122874 * f * f * humidity + 0.00085282 * f * humidity * humidity - 0.00000199 * f * f * humidity * humidity;
        return Math.Max(temperature, (index - 32) * 5 / 9);
    }
}