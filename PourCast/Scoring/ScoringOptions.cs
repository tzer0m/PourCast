namespace PourCast.Scoring;

/// <summary>
/// Configuration for recording and scoring forecasts.
/// </summary>
public class ScoringOptions
{
    /// <summary>
    /// The name of the place forecasts are recorded for.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude forecasts are recorded for, the METAR station's own.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude forecasts are recorded for, the METAR station's own.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The ICAO code of the METAR station observations come from.
    /// </summary>
    public string Station { get; set; } = string.Empty;

    /// <summary>
    /// The base URL of the aviationweather.gov data API.
    /// </summary>
    public string MetarBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// How many hours of METARs to fetch each time, so gaps from restarts are filled in.
    /// </summary>
    public int MetarHours { get; set; } = 24;

    /// <summary>
    /// The UTC hours forecasts are recorded at.
    /// </summary>
    public List<int> SnapshotHours { get; set; } = [];

    /// <summary>
    /// How many days ahead of each snapshot to record.
    /// </summary>
    public int SnapshotDays { get; set; } = 7;

    /// <summary>
    /// How long to wait before retrying a snapshot that failed, in minutes.
    /// </summary>
    public int RetryMinutes { get; set; } = 10;

    /// <summary>
    /// The source name the combined forecast is recorded under.
    /// </summary>
    public string CombinedSource { get; set; } = string.Empty;
}