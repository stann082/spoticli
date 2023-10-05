﻿using System.Text;
using cli.commands.sandbox;
using cli.options;
using core.services;
using SpotifyAPI.Web;

namespace cli.commands;

public static class PlaylistsCommand
{

    #region Public Methods

    public static async Task<int> Execute(PlaylistsOptions options, ISpotifyService spotifyService)
    {
        spotifyService.EnsureUserLoggedIn(out var spotify);

        var page = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 });
        var playlists = await spotify.PaginateAll(page);
        if (!string.IsNullOrEmpty(options.Query))
        {
            playlists = playlists.Where(p => p.Name.Contains(options.Query, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        if (options.Tracks)
        {
            StringBuilder tracksInfo = new StringBuilder();

            List<FullTrack> fullTracks = new List<FullTrack>();
            foreach (var playlist in playlists)
            {
                var pPage = await spotify.Playlists.GetItems(playlist.Id);
                var playlistTracks = (await spotify.PaginateAll(pPage)).Select(p => p.Track);
                foreach (IPlayableItem playlistTrack in playlistTracks)
                {
                    if (playlistTrack is not FullTrack fullTrack)
                    {
                        continue;
                    }

                    tracksInfo.Append($"[{fullTrack.Name}],[{string.Join(", ", fullTrack.Artists.Select(a => a.Name))}]");
                    if (options.ShowTrackId)
                    {
                        tracksInfo.Append($",[{fullTrack.Id}]");
                    }

                    tracksInfo.AppendLine();
                    fullTracks.Add(fullTrack);
                }
            }

            if (!string.IsNullOrEmpty(options.NewPlaylist))
            {
                return await new SandboxHelper().DoStuff(spotify, fullTracks.ToArray());
            }

            Console.WriteLine(string.Join("\r\n", tracksInfo));
            return 0;
        }

        foreach (var playlist in playlists)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(playlist.Name);

            if (options.ShowPlaylistId)
            {
                sb.Append($",{playlist.Id}");
            }

            Console.WriteLine(sb.ToString());
        }

        return 0;
    }

    #endregion

}
