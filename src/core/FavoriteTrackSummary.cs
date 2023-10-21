using SpotifyAPI.Web;

namespace core;

public class FavoriteTrackSummary
{

    #region Constructors

    // ReSharper disable once UnusedMember.Local
    public FavoriteTrackSummary()
    {
        // for deserialization
    }

    public FavoriteTrackSummary(FullTrack[] tracks, SimplePlaylist playlist)
    {
        Tracks = tracks.Select(t => new PlaylistTrack(t)).ToArray();
        TracksUris = tracks.Select(t => new PlaylistRemoveItemsRequest.Item { Uri = t.Uri }).ToArray();
        PlaylistId = playlist.Id;
        PlaylistName = playlist.Name;
    }

    #endregion

    #region Properties

    public PlaylistTrack[] Tracks { get; set; }
    public PlaylistRemoveItemsRequest.Item[] TracksUris { get; set; }
    public string PlaylistId { get; set; }
    public string PlaylistName { get; set; }

    #endregion

}
