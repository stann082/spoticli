using System.Text;
using SpotifyAPI.Web;

namespace core;

public static class ExtensionMethods
{

    #region Constants

    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    #endregion
    
    #region Public Methods

    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        var batch = new List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count != batchSize)
            {
                continue;
            }

            yield return batch;
            batch = new List<T>(batchSize);
        }

        if (batch.Any())
        {
            yield return batch;
        }
    }

    public static FullTrack[] FindTracks(this IEnumerable<PlaylistTrack<IPlayableItem>> sourceItems, IEnumerable<SavedTrack> targetItems)
    {
        var targetTrackIds = new HashSet<string>(targetItems.Select(i => i.Track.Id));
        return sourceItems
            .Select(s => s.Track as FullTrack)
            .Where(t => t != null && targetTrackIds.Contains(t.Id)).ToArray();
    }

    public static DateTime FromEpochTime(this long unixTimestamp, bool localTime = true)
    {
        DateTime utcDateTime = Epoch.AddMilliseconds(unixTimestamp);
        return !localTime ? utcDateTime : utcDateTime.ToLocalTime();
    }
    
    public static string GetTrackSummary(this IEnumerable<PlaylistTrack> tracks)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var track in tracks)
        {
            sb.Append($"\"{track.Name} ({track.Artist})\", ");
        }

        return sb.ToString().TrimEnd().TrimEnd(',');
    }

    public static long ToEpochTime(this DateTime dateTime)
    {
        return (long)(dateTime - Epoch).TotalMilliseconds;
    }

    #endregion

}
