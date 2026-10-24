namespace PourCast.Providers.MetOffice.Models;

/// <summary>
/// GeoJSON response from the Met Office site-specific API.
/// </summary>
/// <typeparam name="T">The time series entry type.</typeparam>
public class MetOfficeResponse<T>
{
    /// <summary>
    /// The features in the collection.
    /// </summary>
    public List<MetOfficeFeature<T>> Features { get; set; } = [];
}