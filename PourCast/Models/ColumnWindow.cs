namespace PourCast.Models;

/// <summary>
/// A window of time that forms one chart column.
/// </summary>
public class ColumnWindow
{
    /// <summary>
    /// The start of the window.
    /// </summary>
    public DateTimeOffset Start { get; set; }

    /// <summary>
    /// The end of the window (exclusive).
    /// </summary>
    public DateTimeOffset End { get; set; }
}