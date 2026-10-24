namespace PourCast.Services.Usage;

/// <summary>
/// The saved call counts for one UTC day.
/// </summary>
public class UsageFile
{
    /// <summary>
    /// The UTC day the counts are for.
    /// </summary>
    public DateOnly Day { get; set; }

    /// <summary>
    /// The number of calls made per usage key.
    /// </summary>
    public Dictionary<string, int> Counts { get; set; } = [];
}