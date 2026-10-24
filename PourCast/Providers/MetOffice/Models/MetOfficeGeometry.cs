namespace PourCast.Providers.MetOffice.Models;

/// <summary>
/// GeoJSON point geometry from the Met Office.
/// </summary>
public class MetOfficeGeometry
{
    /// <summary>
    /// The longitude, latitude and altitude of the point.
    /// </summary>
    public List<double> Coordinates { get; set; } = [];
}