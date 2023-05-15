using cli.options;
using core.services;
using SpotifyAPI.Web;

namespace cli.commands;

public static class PlaylistsCommand
{

    #region Public Methods

    public static int Execute(PlaylistsOptions options, ISpotifyService spotifyService)
    {
        spotifyService.EnsureUserLoggedIn(out var spotify);
        var page = spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 }).GetAwaiter().GetResult();
        var playlists = spotify.PaginateAll(page).GetAwaiter().GetResult().ToArray();
        foreach (var playlist in playlists)
        {
            Console.WriteLine(playlist.Name);
        }
        return 0;
    }

    #endregion

}
