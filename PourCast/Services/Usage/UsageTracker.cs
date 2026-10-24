using Microsoft.Extensions.Options;
using System.Text.Json;
using IOFile = System.IO.File;

namespace PourCast.Services.Usage;

/// <summary>
/// Counts API calls per usage key for the current UTC day, saving them to a JSON file so they survive restarts.
/// </summary>
public class UsageTracker
{
    /// <summary>
    /// Lock guarding the counts, as calls are recorded from many requests at once.
    /// </summary>
    private readonly Lock Gate = new();

    /// <summary>
    /// The full path of the file the counts are saved to.
    /// </summary>
    private readonly string FilePath;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<UsageTracker> Logger;

    /// <summary>
    /// Today's call counts per usage key.
    /// </summary>
    private Dictionary<string, int> Counts = [];

    /// <summary>
    /// The UTC day the counts are for.
    /// </summary>
    private DateOnly Day = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Creates the tracker and loads today's counts from the file, if it has any.
    /// </summary>
    /// <param name="options">The usage options.</param>
    /// <param name="environment">The hosting environment, for resolving the file path.</param>
    /// <param name="logger">The logger.</param>
    public UsageTracker(IOptions<UsageOptions> options, IHostEnvironment environment, ILogger<UsageTracker> logger)
    {
        Logger = logger;
        FilePath = Path.IsPathRooted(options.Value.FilePath) ? options.Value.FilePath : Path.Combine(environment.ContentRootPath, options.Value.FilePath);
        Load();
    }

    /// <summary>
    /// Records a call if it is within the daily limit.
    /// </summary>
    /// <param name="key">The usage key, shared by providers on the same API account.</param>
    /// <param name="limit">The daily limit, or null for no limit.</param>
    public bool TryRecord(string key, int? limit)
    {
        lock (Gate)
        {
            ResetIfNewDay();
            int count = Counts.GetValueOrDefault(key);
            if (limit is not null && count >= limit)
            {
                return false;
            }
            Counts[key] = count + 1;
            Save();
            return true;
        }
    }

    /// <summary>
    /// Returns the number of calls made today for a usage key.
    /// </summary>
    /// <param name="key">The usage key.</param>
    public int GetCount(string key)
    {
        lock (Gate)
        {
            ResetIfNewDay();
            return Counts.GetValueOrDefault(key);
        }
    }

    /// <summary>
    /// Clears the counts when the UTC day changes, matching when providers reset their limits.
    /// </summary>
    private void ResetIfNewDay()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today != Day)
        {
            Counts = [];
            Day = today;
        }
    }

    /// <summary>
    /// Loads the saved counts, ignoring them if they are from a previous day.
    /// </summary>
    private void Load()
    {
        try
        {
            if (!IOFile.Exists(FilePath))
            {
                return;
            }
            UsageFile? saved = JsonSerializer.Deserialize<UsageFile>(IOFile.ReadAllText(FilePath));
            if (saved is not null && saved.Day == Day)
            {
                Counts = saved.Counts;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Logger.LogWarning(ex, "Couldn't read usage file {Path}; starting from zero.", FilePath);
        }
    }

    /// <summary>
    /// Saves the counts. Calls are infrequent, so the file is simply rewritten each time.
    /// </summary>
    private void Save()
    {
        try
        {
            IOFile.WriteAllText(FilePath, JsonSerializer.Serialize(new UsageFile { Day = Day, Counts = Counts }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.LogWarning(ex, "Couldn't write usage file {Path}.", FilePath);
        }
    }
}