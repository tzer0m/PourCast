using PourCast.Models;

namespace PourCast.Services;

/// <summary>
/// Calculates sunrise and sunset times using the standard sunrise equation.
/// </summary>
public static class SunCalculator
{
    /// <summary>
    /// Calculates sunrise and sunset for a run of days.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="start">The first UTC date.</param>
    /// <param name="days">The number of days.</param>
    public static List<Daylight> Calculate(double latitude, double longitude, DateOnly start, int days)
    {
        return Enumerable.Range(0, days).Select(x => Calculate(latitude, longitude, start.AddDays(x))).ToList();
    }

    /// <summary>
    /// Calculates sunrise and sunset for a single day.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="date">The UTC date.</param>
    public static Daylight Calculate(double latitude, double longitude, DateOnly date)
    {
        // Days since J2000 and mean solar time at the location.
        double days = date.DayNumber - new DateOnly(2000, 1, 1).DayNumber + 0.0008;
        double meanSolarTime = days - longitude / 360;

        // Solar mean anomaly, equation of the centre and ecliptic longitude.
        double anomaly = Normalise(357.5291 + 0.98560028 * meanSolarTime);
        double centre = 1.9148 * Sin(anomaly) + 0.02 * Sin(2 * anomaly) + 0.0003 * Sin(3 * anomaly);
        double eclipticLongitude = Normalise(anomaly + centre + 180 + 102.9372);

        // Solar transit, declination and hour angle.
        double transit = 2451545.0 + meanSolarTime + 0.0053 * Sin(anomaly) - 0.0069 * Sin(2 * eclipticLongitude);
        double declination = Math.Asin(Sin(eclipticLongitude) * Sin(23.4397)) * 180 / Math.PI;
        double cosHourAngle = (Sin(-0.833) - Sin(latitude) * Sin(declination)) / (Cos(latitude) * Cos(declination));
        if (cosHourAngle < -1 || cosHourAngle > 1)
        {
            return new Daylight { Date = date, AlwaysUp = cosHourAngle < -1 };
        }
        double hourAngle = Math.Acos(cosHourAngle) * 180 / Math.PI;
        return new Daylight { Date = date, Sunrise = FromJulian(transit - hourAngle / 360), Sunset = FromJulian(transit + hourAngle / 360) };
    }

    /// <summary>
    /// Converts a Julian date to a UTC time.
    /// </summary>
    /// <param name="julian">The Julian date.</param>
    private static DateTimeOffset FromJulian(double julian)
    {
        DateTimeOffset epoch = new(2000, 1, 1, 12, 0, 0, TimeSpan.Zero);
        return epoch.AddDays(julian - 2451545.0);
    }

    /// <summary>
    /// Normalises an angle to the range 0 to 360 degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    private static double Normalise(double degrees)
    {
        double result = degrees % 360;
        return result < 0 ? result + 360 : result;
    }

    /// <summary>
    /// Sine of an angle in degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    private static double Sin(double degrees)
    {
        return Math.Sin(degrees * Math.PI / 180);
    }

    /// <summary>
    /// Cosine of an angle in degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    private static double Cos(double degrees)
    {
        return Math.Cos(degrees * Math.PI / 180);
    }
}