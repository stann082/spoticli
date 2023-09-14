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
            return await RemoveFavoriteTracksFromPlaylists(spotify, savedTracks.ToArray(), me.Id);
        }

        if (options.Delete)
        {
            IList<string> ids = savedTracks.Select(t => t.Track.Id).ToList();
            bool removed = spotify.Library.RemoveTracks(new LibraryRemoveTracksRequest(ids)).GetAwaiter().GetResult();
            if (!removed)
            {
                Console.WriteLine("Could not remove saved tracks");
                return 1;
            }

            Console.WriteLine("The following tracks has been removed from Favorites:");
        }

        foreach (SavedTrack savedTrack in savedTracks)
        {
            Console.WriteLine(GetTracksInfo(new[] { savedTrack.Track }));
        }

        return 0;
    }

    #endregion

    #region Helper Methods

    private static async Task<int> RemoveFavoriteTracksFromPlaylists(ISpotifyClient spotify, SavedTrack[] savedTracks, string myId)
    {
        var playlistsPage = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 });
        var playlists = await spotify.PaginateAll(playlistsPage);
        var myPlaylists = playlists.Where(p => p.Owner.Id == myId).ToArray();
        foreach (var playlist in myPlaylists)
        {
            var page = spotify.Playlists.GetItems(playlist.Id).GetAwaiter().GetResult();
            var playlistTracks = spotify.PaginateAll(page).GetAwaiter().GetResult().ToArray();
            var tracks = playlistTracks.FindTracks(savedTracks);
            if (!tracks.Any())
            {
                continue;
            }

            Console.WriteLine($"Deleting tracks {GetTracksInfo(tracks)} from the playlist {playlist.Name}");
            PlaylistRemoveItemsRequest.Item[] trackUris = tracks.Select(t => new PlaylistRemoveItemsRequest.Item { Uri = t.Uri }).ToArray();
            await spotify.Playlists.RemoveItems(playlist.Id, new PlaylistRemoveItemsRequest
            {
                Tracks = trackUris
            });
        }

        return 0;
    }

    private static string GetTracksInfo(IEnumerable<FullTrack> tracks)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var track in tracks)
        {
            sb.Append($"\"{track.Name} ({string.Join(',', track.Artists.Select(a => a.Name))})\", ");
        }

        return sb.ToString().TrimEnd().TrimEnd(',');
    }

    #endregion

}
