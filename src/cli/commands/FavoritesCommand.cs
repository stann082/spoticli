using System.Text;
using cli.options;
using core;
using core.services;
using SpotifyAPI.Web;

namespace cli.commands;

public static class FavoritesCommand
{

    #region Public Methods

    public static int Execute(FavoritesOptions options, ISpotifyService spotifyService)
    {
        spotifyService.EnsureUserLoggedIn(out var spotify);
        var me = spotify.UserProfile.Current().GetAwaiter().GetResult();
        var tracksPage = spotify.Library.GetTracks().GetAwaiter().GetResult();
        var savedTracks = spotify.PaginateAll(tracksPage).GetAwaiter().GetResult().ToArray();
        if (!savedTracks.Any())
        {
            Console.WriteLine("No saved tracks found");
            return 0;
        }

        foreach (SavedTrack savedTrack in savedTracks)
        {
            Console.WriteLine(GetTracksInfo(new[] { savedTrack.Track }));
        }
        
        return options.RemoveFromPlaylists ? RemoveFavoriteTracksFromPlaylists(spotify, savedTracks, me.Id) : 0;
    }

    #endregion
    
    #region Helper Methods

    private static int RemoveFavoriteTracksFromPlaylists(ISpotifyClient spotify, SavedTrack[] savedTracks, string myId)
    {
        var playlistsPage = spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 }).GetAwaiter().GetResult();
        var playlists = spotify.PaginateAll(playlistsPage).GetAwaiter().GetResult().ToArray();
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
            spotify.Playlists.RemoveItems(playlist.Id, new PlaylistRemoveItemsRequest
            {
                Tracks = trackUris
            }).GetAwaiter().GetResult();
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
