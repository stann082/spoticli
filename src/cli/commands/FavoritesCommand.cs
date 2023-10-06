using System.Text;
using cli.options;
using core;
using core.services;
using SpotifyAPI.Web;

namespace cli.commands;

public static class FavoritesCommand
{

    #region Public Methods

    public static async Task<int> Execute(FavoritesOptions options, ISpotifyService spotifyService)
    {
        spotifyService.EnsureUserLoggedIn(out var spotify);
        var me = await spotify.UserProfile.Current();
        var tracksPage = await spotify.Library.GetTracks();
        var savedTracks = await spotify.PaginateAll(tracksPage);
        if (!savedTracks.Any())
        {
            Console.WriteLine("No saved tracks found");
            return 0;
        }

        if (options.RemoveFromPlaylists)
        {
            return await RemoveFavoriteTracksFromPlaylists(spotify, savedTracks.ToArray(), me.Id, options);
        }

        if (options.Delete)
        {
            if (!options.IsDryRun)
            {
                IList<string> ids = savedTracks.Select(t => t.Track.Id).ToList();
                bool removed = await spotify.Library.RemoveTracks(new LibraryRemoveTracksRequest(ids));
                if (!removed)
                {
                    Console.WriteLine("Could not remove saved tracks");
                    return 1;
                }

                Console.WriteLine("The following tracks has been removed from Favorites:");
            }
            else
            {
                Console.WriteLine("The following tracks will be removed from Favorites:");
            }
        }

        foreach (SavedTrack savedTrack in savedTracks)
        {
            Console.WriteLine(GetTrackSummary(new[] { savedTrack.Track }));
        }

        return 0;
    }

    #endregion

    #region Helper Methods

    private static async Task<int> RemoveFavoriteTracksFromPlaylists(ISpotifyClient spotify, SavedTrack[] savedTracks, string myId, AbstractOption options)
    {
        var playlistsPage = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 });
        var playlists = await spotify.PaginateAll(playlistsPage);
        var myPlaylists = playlists.Where(p => p.Owner.Id == myId).ToArray();

        List<FavoriteTrackInfo> tracks = new List<FavoriteTrackInfo>();

        IEnumerable<IEnumerable<SimplePlaylist>> batches = myPlaylists.Batch(options.BatchSize);
        var tasks = batches.Select(async batch =>
        {
            foreach (var playlist in batch)
            {
                var page = await spotify.Playlists.GetItems(playlist.Id);
                var allPlaylistTracks = await spotify.PaginateAll(page);
                var tracksInPlaylists = allPlaylistTracks.FindTracks(savedTracks);
                if (!tracksInPlaylists.Any())
                {
                    continue;
                }

                tracks.Add(new FavoriteTrackInfo(tracksInPlaylists, playlist));
            }
        });

        await Task.WhenAll(tasks);
        foreach (var trackInfo in tracks)
        {
            string trackSummary = GetTrackSummary(trackInfo.Tracks);
            if (options.IsDryRun)
            {
                Console.WriteLine($"The tracks {trackSummary} will be deleted from the playlist {trackInfo.PlaylistName}");
            }
            else
            {
                Console.WriteLine($"Deleting tracks {trackSummary} from the playlist {trackInfo.PlaylistName}");
                await spotify.Playlists.RemoveItems(trackInfo.PlaylistId, new PlaylistRemoveItemsRequest { Tracks = trackInfo.TracksUris });
            }
        }

        return 0;
    }

    private static string GetTrackSummary(IEnumerable<FullTrack> tracks)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var track in tracks)
        {
            sb.Append($"\"{track.Name} ({string.Join(',', track.Artists.Select(a => a.Name))})\", ");
        }

        return sb.ToString().TrimEnd().TrimEnd(',');
    }

    #endregion

    #region Helper Classes

    private class FavoriteTrackInfo
    {
        public FavoriteTrackInfo(FullTrack[] tracks, SimplePlaylist playlist)
        {
            Tracks = tracks;
            TracksUris = tracks.Select(t => new PlaylistRemoveItemsRequest.Item { Uri = t.Uri }).ToArray();
            PlaylistId = playlist.Id;
            PlaylistName = playlist.Name;
        }

        public FullTrack[] Tracks { get; }
        public PlaylistRemoveItemsRequest.Item[] TracksUris { get; }
        public string PlaylistId { get; }
        public string PlaylistName { get; }

    }

    #endregion

}
