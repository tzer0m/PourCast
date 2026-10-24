namespace PourCast.Providers.MetOffice.Models;

/// <summary>
/// A single forecast point in a Met Office response.
/// </summary>
/// <typeparam name="T">The time series entry type.</typeparam>
public class MetOfficeFeature<T>
{
    /// <summary>
    /// The geometry of the forecast point.
    /// </summary>
    public MetOfficeGeometry? Geometry { get; set; }

    /// <summary>
    /// The properties of the forecast point.
    /// </summary>
    public MetOfficeProperties<T>? Properties { get; set; }
}