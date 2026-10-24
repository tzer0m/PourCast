using PourCast.Models;

namespace PourCast.Services;

/// <summary>
/// Combines forecasts into columns: first each source (or ensemble member) is summarised over the column, then the sources are compared.
/// </summary>
public static class ForecastCombiner
{
    /// <summary>
    /// Builds a combined summary for each column window.
    /// </summary>
    /// <param name="forecasts">The forecasts to combine.</param>
    /// <param name="windows">The column windows, in time order.</param>
    /// <param name="options">How the band is trimmed.</param>
    public static List<HourlySummary> Combine(List<Forecast> forecasts, List<ColumnWindow> windows, CombiningOptions options)
    {
        // Ensembles always use their percentiles; normal sources use percentiles once there are enough of them, otherwise min to max.
        bool ensemble = forecasts.Any(x => x.IsEnsemble);
        List<DateTimeOffset> starts = windows.Select(x => x.Start).ToList();

        // Each series is one source, or one member of an ensemble, with its hours bucketed by column.
        List<Dictionary<int, List<HourlyForecast>>> series = forecasts.SelectMany(x => x.Hourly.GroupBy(h => h.Member ?? string.Empty)).Select(x => Bucket(options.CalculateFeelsLike ? x.Select(h => WithCalculatedFeelsLike(h, options)) : x, windows, starts)).ToList();
        List<HourlySummary> result = [];
        for (int i = 0; i < windows.Count; i++)
        {
            List<HourlyForecast> values = series.Where(x => x.ContainsKey(i)).Select(x => Aggregate(x[i])).ToList();
            if (values.Count > 0)
            {
                result.Add(Summarise(values, windows[i], ensemble, options));
            }
        }
        return result;
    }

    /// <summary>
    /// Returns a copy of the hour with feels like calculated from its own temperature, wind and humidity.
    /// </summary>
    /// <param name="hour">The source's hour.</param>
    /// <param name="options">The feels like settings.</param>
    private static HourlyForecast WithCalculatedFeelsLike(HourlyForecast hour, CombiningOptions options)
    {
        HourlyForecast copy = hour.Copy();
        copy.FeelsLike = FeelsLikeCalculator.Calculate(hour.Temperature, hour.WindSpeed, hour.Humidity, options);
        return copy;
    }

    /// <summary>
    /// Groups a series' hours by the column they fall in.
    /// </summary>
    /// <param name="hours">The series' hours.</param>
    /// <param name="windows">The column windows.</param>
    /// <param name="starts">The column start times, for searching.</param>
    private static Dictionary<int, List<HourlyForecast>> Bucket(IEnumerable<HourlyForecast> hours, List<ColumnWindow> windows, List<DateTimeOffset> starts)
    {
        Dictionary<int, List<HourlyForecast>> buckets = [];
        foreach (HourlyForecast hour in hours)
        {
            int index = starts.BinarySearch(hour.Time);
            if (index < 0)
            {
                index = ~index - 1;
            }
            if (index < 0 || hour.Time >= windows[index].End)
            {
                continue;
            }
            if (!buckets.TryGetValue(index, out List<HourlyForecast>? bucket))
            {
                bucket = [];
                buckets[index] = bucket;
            }
            bucket.Add(hour);
        }
        return buckets;
    }

    /// <summary>
    /// Summarises one series over a column: averages for temperature, humidity and wind, a total for rain, and the peak for chance, UV and gusts.
    /// </summary>
    /// <param name="hours">The series' hours in the column.</param>
    private static HourlyForecast Aggregate(List<HourlyForecast> hours)
    {
        if (hours.Count == 1)
        {
            return hours[0];
        }
        return new HourlyForecast
        {
            Time = hours[0].Time,
            Condition = Mode(hours.Select(x => x.Condition)),
            Temperature = Mean(hours.Select(x => x.Temperature)),
            FeelsLike = Mean(hours.Select(x => x.FeelsLike)),
            Humidity = Mean(hours.Select(x => x.Humidity)),
            CloudCover = Mean(hours.Select(x => x.CloudCover)),
            WindSpeed = Mean(hours.Select(x => x.WindSpeed)),
            WindGust = Peak(hours.Select(x => x.WindGust)),
            WindDirection = Direction(hours.Select(x => x.WindDirection)),
            Precipitation = Total(hours.Select(x => x.Precipitation)),
            PrecipitationProbability = Peak(hours.Select(x => x.PrecipitationProbability)),
            UvIndex = Peak(hours.Select(x => x.UvIndex)),
            UvIndexClearSky = Peak(hours.Select(x => x.UvIndexClearSky))
        };
    }

    /// <summary>
    /// Compares the series' values for a column, giving the average and the spread for each metric.
    /// </summary>
    /// <param name="values">One summarised value per series.</param>
    /// <param name="window">The column window.</param>
    /// <param name="ensemble">Whether the values are ensemble members.</param>
    /// <param name="options">How the band is trimmed.</param>
    private static HourlySummary Summarise(List<HourlyForecast> values, ColumnWindow window, bool ensemble, CombiningOptions options)
    {
        (double? Average, double? Low, double? High) temperature = Spread(values.Select(x => x.Temperature), ensemble, options);
        (double? Average, double? Low, double? High) feelsLike = Spread(values.Select(x => x.FeelsLike), ensemble, options);
        (double? Average, double? Low, double? High) humidity = Spread(values.Select(x => x.Humidity), ensemble, options);
        (double? Average, double? Low, double? High) windSpeed = Spread(values.Select(x => x.WindSpeed), ensemble, options);
        (double? Average, double? Low, double? High) windGust = Spread(values.Select(x => x.WindGust), ensemble, options);
        (double? Average, double? Low, double? High) uvIndex = Spread(values.Select(x => x.UvIndex), ensemble, options);
        (double? Average, double? Low, double? High) uvIndexClearSky = Spread(values.Select(x => x.UvIndexClearSky), ensemble, options);
        (double? Average, double? Low, double? High) precipitation = Spread(values.Select(x => x.Precipitation), ensemble, options);
        (double? Average, double? Low, double? High) probability = Spread(values.Select(x => x.PrecipitationProbability), ensemble, options);
        return new HourlySummary
        {
            Time = window.Start,
            End = window.End,
            SourceCount = values.Count,
            Condition = Mode(values.Select(x => x.Condition)),
            TemperatureAverage = temperature.Average,
            TemperatureMin = temperature.Low,
            TemperatureMax = temperature.High,
            FeelsLikeAverage = feelsLike.Average,
            FeelsLikeMin = feelsLike.Low,
            FeelsLikeMax = feelsLike.High,
            HumidityAverage = humidity.Average,
            HumidityMin = humidity.Low,
            HumidityMax = humidity.High,
            WindSpeedAverage = windSpeed.Average,
            WindSpeedMin = windSpeed.Low,
            WindSpeedMax = windSpeed.High,
            WindGustAverage = windGust.Average,
            WindGustMin = windGust.Low,
            WindGustMax = windGust.High,
            WindDirectionAverage = Direction(values.Select(x => x.WindDirection)),
            UvIndexAverage = uvIndex.Average,
            UvIndexMin = uvIndex.Low,
            UvIndexMax = uvIndex.High,
            UvIndexClearSkyAverage = uvIndexClearSky.Average,
            UvIndexClearSkyMin = uvIndexClearSky.Low,
            UvIndexClearSkyMax = uvIndexClearSky.High,
            PrecipitationAverage = precipitation.Average,
            PrecipitationMin = precipitation.Low,
            PrecipitationMax = precipitation.High,
            PrecipitationProbabilityAverage = probability.Average,
            PrecipitationProbabilityMin = ensemble ? null : probability.Low,
            PrecipitationProbabilityMax = ensemble ? null : probability.High
        };
    }

    /// <summary>
    /// Returns the average and the spread of the values, or nulls if there are none.
    /// </summary>
    /// <param name="values">The values, which may include nulls.</param>
    /// <param name="ensemble">Whether the values are ensemble members.</param>
    /// <param name="options">How the band is trimmed.</param>
    private static (double? Average, double? Low, double? High) Spread(IEnumerable<double?> values, bool ensemble, CombiningOptions options)
    {
        List<double> list = values.Where(x => x is not null).Select(x => x!.Value).Order().ToList();
        if (list.Count == 0)
        {
            return (null, null, null);
        }
        if (ensemble)
        {
            return (list.Average(), Percentile(list, options.EnsembleLowPercentile), Percentile(list, options.EnsembleHighPercentile));
        }
        if (list.Count >= options.PercentileMinimumSources)
        {
            return (list.Average(), Percentile(list, options.LowPercentile), Percentile(list, options.HighPercentile));
        }
        return (list.Average(), list[0], list[^1]);
    }

    /// <summary>
    /// Returns a percentile of sorted values, interpolating between neighbours.
    /// </summary>
    /// <param name="sorted">The values in ascending order.</param>
    /// <param name="fraction">The percentile as a fraction, e.g. 0.1 for the 10th.</param>
    private static double Percentile(List<double> sorted, double fraction)
    {
        double position = fraction * (sorted.Count - 1);
        int lower = (int)Math.Floor(position);
        int upper = Math.Min(lower + 1, sorted.Count - 1);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    /// <summary>
    /// Returns the mean of the values, or null if there are none.
    /// </summary>
    /// <param name="values">The values, which may include nulls.</param>
    private static double? Mean(IEnumerable<double?> values)
    {
        List<double> list = values.Where(x => x is not null).Select(x => x!.Value).ToList();
        return list.Count > 0 ? list.Average() : null;
    }

    /// <summary>
    /// Returns the total of the values, or null if there are none.
    /// </summary>
    /// <param name="values">The values, which may include nulls.</param>
    private static double? Total(IEnumerable<double?> values)
    {
        List<double> list = values.Where(x => x is not null).Select(x => x!.Value).ToList();
        return list.Count > 0 ? list.Sum() : null;
    }

    /// <summary>
    /// Returns the highest of the values, or null if there are none.
    /// </summary>
    /// <param name="values">The values, which may include nulls.</param>
    private static double? Peak(IEnumerable<double?> values)
    {
        List<double> list = values.Where(x => x is not null).Select(x => x!.Value).ToList();
        return list.Count > 0 ? list.Max() : null;
    }

    /// <summary>
    /// Returns the circular mean of compass directions, or null if there are none.
    /// </summary>
    /// <param name="degrees">The directions in degrees, which may include nulls.</param>
    private static double? Direction(IEnumerable<double?> degrees)
    {
        List<double> list = degrees.Where(x => x is not null).Select(x => x!.Value).ToList();
        if (list.Count == 0)
        {
            return null;
        }
        double sin = list.Average(x => Math.Sin(x * Math.PI / 180));
        double cos = list.Average(x => Math.Cos(x * Math.PI / 180));
        double result = Math.Atan2(sin, cos) * 180 / Math.PI;
        return result < 0 ? result + 360 : result;
    }

    /// <summary>
    /// Returns the most common known condition, or Unknown if there are none.
    /// </summary>
    /// <param name="conditions">The conditions.</param>
    private static WeatherCondition Mode(IEnumerable<WeatherCondition> conditions)
    {
        return conditions.Where(x => x != WeatherCondition.Unknown).GroupBy(x => x).OrderByDescending(x => x.Count()).Select(x => x.Key).FirstOrDefault();
    }
}