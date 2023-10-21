using cli.options;
using core;
using core.config;
using core.services;
using Newtonsoft.Json;
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
            Console.WriteLine(new[] { savedTrack.Track }.Select(t => new PlaylistTrack(t)).GetTrackSummary());
        }

        return 0;
    }

    #endregion

    #region Helper Methods

    private static async Task LogDeletedTracks(IEnumerable<FavoriteTrackSummary> tracks, bool isDryRun)
    {
        if (isDryRun)
        {
            return;
        }

        long unixTimestamp = DateTime.UtcNow.ToEpochTime();
        string folderName = Path.Combine(ApplicationConfig.AppConfigRootPath, "deleted", $"{unixTimestamp}");
        if (!Directory.Exists(folderName))
        {
            Directory.CreateDirectory(folderName);
        }

        string json = JsonConvert.SerializeObject(tracks);
        await using StreamWriter outputFile = new StreamWriter(Path.Combine(folderName, Constants.FavoritesFileName));
        await outputFile.WriteAsync(json);
    }

    private static async Task<int> RemoveFavoriteTracksFromPlaylists(ISpotifyClient spotify, SavedTrack[] savedTracks, string myId, AbstractOption options)
    {
        var playlistsPage = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 });
        var playlists = await spotify.PaginateAll(playlistsPage);
        var myPlaylists = playlists.Where(p => p.Owner.Id == myId).ToArray();

        List<FavoriteTrackSummary> tracks = new List<FavoriteTrackSummary>();

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

                tracks.Add(new FavoriteTrackSummary(tracksInPlaylists, playlist));
            }
        });

        await Task.WhenAll(tasks);
        foreach (var trackInfo in tracks)
        {
            string trackSummary = trackInfo.Tracks.GetTrackSummary();
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

        await LogDeletedTracks(tracks.ToArray(), options.IsDryRun);
        return 0;
    }

    #endregion

}
