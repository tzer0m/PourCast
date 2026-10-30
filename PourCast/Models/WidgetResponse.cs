namespace PourCast.Models;

/// <summary>
/// The compact weather summary shown by the Tidgets home screen widget.
/// </summary>
public class WidgetResponse
{
    /// <summary>
    /// The widget title, usually the place name.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The main value, the current temperature.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The supporting line, the condition with today's high and low.
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;
}