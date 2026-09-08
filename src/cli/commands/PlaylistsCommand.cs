using System.Text;
using System.Text.RegularExpressions;
using cli.commands.sandbox;
using cli.options;
using core;
using core.services;
using Newtonsoft.Json;
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

        if (!string.IsNullOrEmpty(options.FromJson))
        {
            return await CreatePlaylistFromJson(spotify, options);
        }

        if (!string.IsNullOrEmpty(options.NewPlaylist))
        {
            return await CreatePlaylist(spotify, options);
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
            foreach (var batch in tracks.Chunk(100))
            {
                var ids = batch.Select(t => t.Id).ToList();
                var featuresResponse = await spotify.Tracks.GetSeveralAudioFeatures(
                    new TracksAudioFeaturesRequest(ids));
                allFeatures.AddRange(featuresResponse.AudioFeatures.Where(f => f != null));
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

    private static async Task<int> CreatePlaylist(SpotifyClient spotify, PlaylistsOptions options)
    {
        var me = await spotify.UserProfile.Current();
        var newPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest(options.NewPlaylist));
        Console.WriteLine($"Created playlist \"{options.NewPlaylist}\".");

        if (!string.IsNullOrEmpty(options.TrackIdsFile))
        {
            int result = await AddTracksFromIdsFile(spotify, newPlaylist.Id, options);
            if (result != 0)
            {
                return result;
            }
        }

        if (!string.IsNullOrEmpty(options.SearchFile))
        {
            return await AddTracksFromSearchFile(spotify, newPlaylist.Id, options);
        }

        return 0;
    }

    private static async Task<int> CreatePlaylistFromJson(SpotifyClient spotify, PlaylistsOptions options)
    {
        if (!File.Exists(options.FromJson))
        {
            Console.WriteLine($"File not found: {options.FromJson}");
            return 1;
        }

        PlaylistDefinition definition;
        try
        {
            definition = JsonConvert.DeserializeObject<PlaylistDefinition>(File.ReadAllText(options.FromJson));
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Could not parse {options.FromJson}: {ex.Message}");
            return 1;
        }

        if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
        {
            Console.WriteLine("The JSON file must specify a playlist \"name\".");
            return 1;
        }

        var tracks = definition.Tracks ?? new List<TrackDefinition>();

        var me = await spotify.UserProfile.Current();
        var createRequest = new PlaylistCreateRequest(definition.Name);
        if (!string.IsNullOrEmpty(definition.Description))
        {
            createRequest.Description = definition.Description;
        }

        var newPlaylist = await spotify.Playlists.Create(me.Id, createRequest);
        Console.WriteLine($"Created playlist \"{definition.Name}\".");

        var byId = tracks.Where(t => !string.IsNullOrEmpty(t.Id)).ToList();
        var reported = tracks.Where(t => string.IsNullOrEmpty(t.Id) && !string.IsNullOrEmpty(t.Status)).ToList();
        var toSearch = tracks.Where(t => string.IsNullOrEmpty(t.Id) && string.IsNullOrEmpty(t.Status)).ToList();

        var uris = byId
            .Select(t => t.Id.StartsWith("spotify:track:") ? t.Id : $"spotify:track:{t.Id}")
            .ToList();

        var searchEntries = toSearch
            .Select(t => new SearchEntry(FormatTrackDefinition(t), t.Title, t.Artist ?? string.Empty))
            .ToList();

        if (searchEntries.Count > 0)
        {
            Console.WriteLine($"Searching for {searchEntries.Count} track(s)...");

            if (options.RunSynchronously)
            {
                foreach (var entry in searchEntries)
                {
                    await SearchForTrack(spotify, entry);
                }
            }
            else
            {
                foreach (var batch in searchEntries.Batch(options.BatchSize))
                {
                    await Task.WhenAll(batch.Select(entry => SearchForTrack(spotify, entry)));
                }
            }
        }

        uris.AddRange(searchEntries.Where(e => e.Track != null).Select(e => e.Track.Uri));

        foreach (var chunk in uris.Chunk(100))
        {
            await spotify.Playlists.AddItems(newPlaylist.Id, new PlaylistAddItemsRequest(chunk.ToList()));
        }

        Console.WriteLine($"Added {uris.Count} of {byId.Count + searchEntries.Count} track(s).");

        var notFound = searchEntries.Where(e => e.Track == null).ToList();
        if (notFound.Count > 0)
        {
            Console.WriteLine("Could not find the following track(s):");
            foreach (var entry in notFound)
            {
                Console.WriteLine($"  {entry.RawLine}");
            }
        }

        foreach (var group in reported.GroupBy(t => t.Status, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Skipped ({group.Key}):");
            foreach (var t in group)
            {
                Console.WriteLine($"  {FormatTrackDefinition(t)}");
            }
        }

        return 0;
    }

    private static string FormatTrackDefinition(TrackDefinition track)
    {
        return string.IsNullOrEmpty(track.Artist) ? track.Title : $"{track.Title} — {track.Artist}";
    }

    private static async Task<int> AddTracksFromIdsFile(SpotifyClient spotify, string playlistId, PlaylistsOptions options)
    {
        if (!File.Exists(options.TrackIdsFile))
        {
            Console.WriteLine($"File not found: {options.TrackIdsFile}");
            return 1;
        }

        var trackIds = File.ReadAllLines(options.TrackIdsFile)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        if (trackIds.Count == 0)
        {
            Console.WriteLine("No track IDs found in file.");
            return 0;
        }

        foreach (var chunk in trackIds.Chunk(100))
        {
            var uris = chunk.Select(id => $"spotify:track:{id}").ToList();
            await spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(uris));
        }

        Console.WriteLine($"Added {trackIds.Count} track(s).");
        return 0;
    }

    private static async Task<int> AddTracksFromSearchFile(SpotifyClient spotify, string playlistId, PlaylistsOptions options)
    {
        if (!File.Exists(options.SearchFile))
        {
            Console.WriteLine($"File not found: {options.SearchFile}");
            return 1;
        }

        var entries = File.ReadAllLines(options.SearchFile)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
            .Select(ParseSearchEntry)
            .ToList();

        if (entries.Count == 0)
        {
            Console.WriteLine("No entries found in search file.");
            return 0;
        }

        Console.WriteLine($"Searching for {entries.Count} track(s)...");

        if (options.RunSynchronously)
        {
            foreach (var entry in entries)
            {
                await SearchForTrack(spotify, entry);
            }
        }
        else
        {
            foreach (var batch in entries.Batch(options.BatchSize))
            {
                await Task.WhenAll(batch.Select(entry => SearchForTrack(spotify, entry)));
            }
        }

        var found = entries.Where(e => e.Track != null).ToList();
        var notFound = entries.Where(e => e.Track == null).ToList();

        foreach (var chunk in found.Chunk(100))
        {
            var uris = chunk.Select(e => e.Track.Uri).ToList();
            await spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(uris));
        }

        Console.WriteLine($"Added {found.Count} of {entries.Count} track(s).");

        if (notFound.Count > 0)
        {
            Console.WriteLine("Could not find the following track(s):");
            foreach (var entry in notFound)
            {
                Console.WriteLine($"  {entry.RawLine}");
            }
        }

        return 0;
    }

    private static SearchEntry ParseSearchEntry(string line)
    {
        var bracketMatch = Regex.Match(line, @"^\[(.*?)\]\s*,\s*\[(.*?)\]");
        if (bracketMatch.Success)
        {
            return new SearchEntry(line, bracketMatch.Groups[1].Value.Trim(), bracketMatch.Groups[2].Value.Trim());
        }

        var parts = line.Split(',', 2);
        var song = parts[0].Trim();
        var artist = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        return new SearchEntry(line, song, artist);
    }

    private static async Task SearchForTrack(SpotifyClient spotify, SearchEntry entry)
    {
        var query = string.IsNullOrEmpty(entry.Artist)
            ? $"track:{entry.Song}"
            : $"track:{entry.Song} artist:{entry.Artist}";

        var response = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, query));
        entry.Track = response.Tracks?.Items?.FirstOrDefault();
    }

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

    private class SearchEntry
    {
        public SearchEntry(string rawLine, string song, string artist)
        {
            RawLine = rawLine;
            Song = song;
            Artist = artist;
        }

        public string RawLine { get; }
        public string Song { get; }
        public string Artist { get; }
        public FullTrack Track { get; set; }
    }

}
