namespace PourCast.Providers.MetOffice.Models;

/// <summary>
/// Properties of a Met Office forecast point.
/// </summary>
/// <typeparam name="T">The time series entry type.</typeparam>
public class MetOfficeProperties<T>
{
    /// <summary>
    /// The location of the forecast point.
    /// </summary>
    public MetOfficeLocation? Location { get; set; }

    /// <summary>
    /// The distance in metres from the requested point to the forecast point.
    /// </summary>
    public double? RequestPointDistance { get; set; }

    /// <summary>
    /// The time of the model run.
    /// </summary>
    public string? ModelRunDate { get; set; }

    /// <summary>
    /// The forecast time series.
    /// </summary>
    public List<T> TimeSeries { get; set; } = [];
}