using System.Text;
using cli.commands.sandbox;
using cli.options;
using core;
using core.config;
using core.services;
using SpotifyAPI.Web;

namespace cli.commands;

public static class PlaylistsCommand
{

    #region Public Methods

    public static async Task<int> Execute(PlaylistsOptions options, ISpotifyService spotifyService)
    {
        spotifyService.EnsureUserLoggedIn(out var spotify);

        if (!string.IsNullOrEmpty(options.NewPlaylist))
        {
            // return await new SandboxHelper().DoStuff(spotify);
        }

        var page = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 });
        var playlists = await spotify.PaginateAll(page);
        if (!string.IsNullOrEmpty(options.Query))
        {
            playlists = playlists.Where(p => p.Name.Contains(options.Query, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        if (options.Tracks)
        {
            List<string> trackItems = new List<string>();
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

                    StringBuilder sb = new StringBuilder();
                    sb.Append($"[{fullTrack.Name}],[{string.Join(", ", fullTrack.Artists.Select(a => a.Name))}]");

                    if (options.ShowTrackId)
                    {
                        sb.Append($",[{fullTrack.Id}]");
                    }

                    trackItems.Add(sb.ToString());
                }
            }

            if (options.Export)
            {
                const int chunkSize = 20;
                var chunks = trackItems.Batch(chunkSize);
                int fileCount = 0;
                foreach (var chunk in chunks)
                {
                    fileCount++;

                    string exportRootPath = Path.Combine(ApplicationConfig.AppConfigRootPath, "exports");
                    if (!Directory.Exists(exportRootPath))
                    {
                        Directory.CreateDirectory(exportRootPath);
                    }

                    string query = !string.IsNullOrEmpty(options.Query) ? options.Query : "playlist";
                    string filePrefix = query.Replace(' ', '_').ToLower();
                    string filename = Path.Combine(exportRootPath, $"{filePrefix}_{fileCount}.txt");
                    await File.WriteAllLinesAsync(filename, chunk);
                }

                return 0;
            }

            Console.WriteLine(string.Join("\r\n", trackItems));
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
