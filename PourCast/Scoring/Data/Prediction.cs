namespace PourCast.Scoring.Data;

/// <summary>
/// One predicted hour from a snapshot.
/// </summary>
public class Prediction
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public long PredictionId { get; set; }

    /// <summary>
    /// The snapshot this prediction belongs to.
    /// </summary>
    public long SnapshotId { get; set; }

    /// <summary>
    /// The snapshot this prediction belongs to.
    /// </summary>
    public Snapshot? Snapshot { get; set; }

    /// <summary>
    /// The start of the predicted hour, in UTC.
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Hours between the snapshot and the predicted hour.
    /// </summary>
    public int LeadHours { get; set; }

    /// <summary>
    /// Temperature in °C.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Feels like temperature in °C.
    /// </summary>
    public double? FeelsLike { get; set; }

    /// <summary>
    /// Relative humidity in %.
    /// </summary>
    public double? Humidity { get; set; }

    /// <summary>
    /// Wind speed in mph.
    /// </summary>
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in mph.
    /// </summary>
    public double? WindGust { get; set; }

    /// <summary>
    /// Wind direction in degrees.
    /// </summary>
    public double? WindDirection { get; set; }

    /// <summary>
    /// Rain amount in mm.
    /// </summary>
    public double? Precipitation { get; set; }

    /// <summary>
    /// Chance of rain in %.
    /// </summary>
    public double? PrecipitationProbability { get; set; }
}