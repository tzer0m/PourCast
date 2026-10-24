namespace PourCast.Scoring.Data;

/// <summary>
/// A forecast from one source, recorded at a snapshot time so it can be scored later.
/// </summary>
public class Snapshot
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public long SnapshotId { get; set; }

    /// <summary>
    /// The forecast source, e.g. Met Office, or PourCast for the combined average.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// When the forecast was recorded, in UTC.
    /// </summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>
    /// The predicted hours.
    /// </summary>
    public List<Prediction> Predictions { get; set; } = [];
}