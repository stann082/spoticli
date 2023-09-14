using SpotifyAPI.Web;

namespace core;

public static class ExtensionMethods
{

    #region Public Methods

    public static FullTrack[] FindTracks(this IEnumerable<PlaylistTrack<IPlayableItem>> sourceItems, IEnumerable<SavedTrack> targetItems)
    {
        var targetTrackIds = new HashSet<string>(targetItems.Select(i => i.Track.Id));
        return sourceItems
            .Select(s => s.Track as FullTrack)
            .Where(t => t != null && targetTrackIds.Contains(t.Id)).ToArray();
    }

    #endregion
    
}
