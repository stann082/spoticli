using System.Text;
using cli.commands.sandbox;
using cli.options;
using core;
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

        if (options.DjMix && !string.IsNullOrEmpty(options.TrackIdsFile))
        {
            if (string.IsNullOrEmpty(options.PlaylistName))
            {
                Console.WriteLine("Specify a playlist name with --name.");
                return 1;
            }

            if (!File.Exists(options.TrackIdsFile))
            {
                Console.WriteLine($"File not found: {options.TrackIdsFile}");
                return 1;
            }

            var trackIds = File.ReadAllLines(options.TrackIdsFile)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct()
                .ToList();

            if (trackIds.Count == 0)
            {
                Console.WriteLine("No track IDs found in file.");
                return 1;
            }

            Console.WriteLine($"Read {trackIds.Count} track IDs from file.");

            var uris = trackIds.Select(id => $"spotify:track:{id}").ToList();
            var me = await spotify.UserProfile.Current();
            var mixName = $"{options.PlaylistName} [DJ Mix]";
            var newPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest(mixName));
            Console.WriteLine($"Created playlist \"{mixName}\".");

            foreach (var chunk in uris.Chunk(100))
            {
                await spotify.Playlists.AddItems(newPlaylist.Id, new PlaylistAddItemsRequest(chunk.ToList()));
            }

            Console.WriteLine($"Added {uris.Count} tracks. Done.");
            return 0;
        }

        if (options.DjMix)
        {
            if (playlists.Count == 0)
            {
                Console.WriteLine("No matching playlist found.");
                return 1;
            }

            var source = playlists.First();
            Console.WriteLine($"Fetching tracks from \"{source.Name}\"...");

            var pPage = await spotify.Playlists.GetItems(source.Id);
            var playlistItems = await spotify.PaginateAll(pPage);
            var tracks = playlistItems
                .Select(p => p.Track)
                .OfType<FullTrack>()
                .ToList();

            if (tracks.Count == 0)
            {
                Console.WriteLine("Playlist has no tracks.");
                return 1;
            }

            Console.WriteLine($"Fetching audio features for {tracks.Count} tracks...");

            // Batch in chunks of 100 (Spotify API limit)
            var allFeatures = new List<TrackAudioFeatures>();
            try
            {
                foreach (var batch in tracks.Chunk(100))
                {
                    var ids = batch.Select(t => t.Id).ToList();
                    var featuresResponse = await spotify.Tracks.GetSeveralAudioFeatures(
                        new TracksAudioFeaturesRequest(ids));
                    allFeatures.AddRange(featuresResponse.AudioFeatures.Where(f => f != null));
                }
            }
            catch (APIException ex)
            {
                Console.WriteLine($"Failed to fetch audio features: {ex.Message}");
                Console.WriteLine("Spotify restricted this endpoint for apps created after November 2024. Your app credentials may not have access.");
                return 1;
            }

            var featureMap = allFeatures.ToDictionary(f => f.Id);
            var paired = tracks
                .Where(t => featureMap.ContainsKey(t.Id))
                .Select(t => (track: t, features: featureMap[t.Id]))
                .ToList();

            if (paired.Count == 0)
            {
                Console.WriteLine("Could not retrieve audio features. Your Spotify app may not have access to this endpoint.");
                return 1;
            }

            Console.WriteLine("Ordering tracks for smooth DJ transitions...");
            var ordered = DjMixHelper.OrderForDjMix(paired);

            var me = await spotify.UserProfile.Current();
            var mixName = $"{source.Name} [DJ Mix]";
            var newPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest(mixName));
            Console.WriteLine($"Created playlist \"{mixName}\".");

            foreach (var chunk in ordered.Chunk(100))
            {
                var uris = chunk.Select(x => x.track.Uri).ToList();
                await spotify.Playlists.AddItems(newPlaylist.Id, new PlaylistAddItemsRequest(uris));
            }

            Console.WriteLine($"Added {ordered.Count} tracks. Done.");
            return 0;
        }

        if (options.ShouldFindDuplicates)
        {
            var me = await spotify.UserProfile.Current();
            var myPlaylists = playlists.Where(p => p.Owner.Id == me.Id).ToArray();
            await DuplicateFinder.Find(spotify, myPlaylists.ToArray(), options);
            return 0;
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

    private static string GetTrackSummary(IEnumerable<FullTrack> tracks)
    {
        StringBuilder sb = new StringBuilder();

        bool isFirst = true;
        foreach (var track in tracks)
        {
            if (isFirst)
            {
                sb.AppendLine($"\"{track.Name} ({string.Join(',', track.Artists.Select(a => a.Name))})\"");
                isFirst = false;
            }
            else
            {
                sb.AppendLine($"        \"{track.Name} ({string.Join(',', track.Artists.Select(a => a.Name))})\"");
            }
        }

        return sb.ToString();
    }

}
