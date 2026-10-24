using PourCast.Models;

namespace PourCast.Providers;

/// <summary>
/// Lines up rain timing across sources. PourCast treats an hour's rain as falling in the hour starting at its time (a 2pm column means 2pm to 3pm),
/// but some sources label rain with the end of the hour it fell in.
/// </summary>
public static class PrecipitationTiming
{
    /// <summary>
    /// Moves rain values labelled with the end of their hour back onto the hour they fell in.
    /// </summary>
    /// <param name="hours">The hours, from a single source or ensemble member, in time order.</param>
    /// <param name="includeProbability">Whether the chance of rain is labelled the same way and should move too.</param>
    public static void ShiftToHourStart(List<HourlyForecast> hours, bool includeProbability)
    {
        for (int i = 0; i < hours.Count; i++)
        {
            // The value for 2pm to 3pm is the one labelled 3pm, if the next hour is present.
            HourlyForecast? next = i + 1 < hours.Count && hours[i + 1].Time == hours[i].Time.AddHours(1) ? hours[i + 1] : null;
            hours[i].Precipitation = next?.Precipitation;
            if (includeProbability)
            {
                hours[i].PrecipitationProbability = next?.PrecipitationProbability;
            }
        }
    }
}