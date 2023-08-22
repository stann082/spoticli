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
        if (!string.IsNullOrEmpty(options.Query))
        {
            playlists = playlists.Where(p => p.Name.Contains(options.Query)).ToArray();
        }

        if (options.Tracks)
        {
            foreach (var playlist in playlists)
            {
                var pPage = spotify.Playlists.GetItems(playlist.Id).GetAwaiter().GetResult();
                var playlistTracks = spotify.PaginateAll(pPage).GetAwaiter().GetResult().Select(p => p.Track);
                foreach (IPlayableItem playlistTrack in playlistTracks)
                {
                    FullTrack fullTrack = playlistTrack as FullTrack;
                    if (fullTrack == null)
                    {
                        continue;
                    }
                    
                    Console.WriteLine($"[{fullTrack.Name}][{string.Join(", ", fullTrack.Artists.Select(a => a.Name))}]");
                }
            }

            return 0;
        }

        foreach (var playlist in playlists)
        {
            Console.WriteLine(playlist.Name);
        }

        return 0;
    }

    #endregion

}
