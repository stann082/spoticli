using SpotifyAPI.Web;

namespace core;

public static class ExtensionMethods
{

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

    #endregion
    
}
