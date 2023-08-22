using SpotifyAPI.Web;

namespace core;

public static class ExtensionMethods
{

    #region Public Methods

    // public static FullTrack[] FindTracks(this IEnumerable<PlaylistTrack<IPlayableItem>> sourceItems, IEnumerable<SavedTrack> targetItems)
    // {
    //     var sourceTracks = sourceItems.Select(s => s.Track as FullTrack).ToArray();
    //     var targetTracks = targetItems.Select(i => i.Track).ToArray();
    //     var sourceTrackIds = sourceTracks.Select(i => i?.Id);
    //     var targetTrackIds = targetTracks.Select(i => i.Id);
    //     string[] trackIds = sourceTrackIds.Intersect(targetTrackIds).ToArray();
    //     return sourceTracks.Where(t => trackIds.Contains(t.Id)).ToArray();
    // }

    public static FullTrack[] FindTracks(this IEnumerable<PlaylistTrack<IPlayableItem>> sourceItems, IEnumerable<SavedTrack> targetItems)
    {
        var targetTrackIds = new HashSet<string>(targetItems.Select(i => i.Track.Id));
        return sourceItems
            .Select(s => s.Track as FullTrack)
            .Where(t => t != null && targetTrackIds.Contains(t.Id)).ToArray();
    }

    #endregion
    
}
