using System.Text.RegularExpressions;
using core;
using core.config;
using SpotifyAPI.Web;

namespace cli.commands.sandbox;

public class SandboxHelper
{

    #region Public Methods

    public async Task<int> DoStuff(ISpotifyClient spotify)
    {
        string importsPath = Path.Combine(ApplicationConfig.AppConfigRootPath, "imports");
        if (!Directory.Exists(importsPath))
        {
            Console.WriteLine($"Could not find import directory {importsPath}");
            return 1;
        }

        List<TrackInfo> allTracks = new List<TrackInfo>();
        string[] files = Directory.GetFiles(importsPath, "*.txt", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            TrackInfo[] parsedTracks = ParseImportFile(file);
            if (!parsedTracks.Any())
            {
                Console.WriteLine($"Could not parse tracks from file {file}");
                continue;
            }

            allTracks.AddRange(parsedTracks);
        }

        var me = await spotify.UserProfile.Current();
        TrackInfo[] sortedTracks = allTracks.OrderBy(t => t.Year).ToArray();
        var tracks60sAndEarlier = sortedTracks.Where(t => t.Year <= 1969).ToArray();
        Shuffle(tracks60sAndEarlier);
        var sixtiesPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest("Classic Vinyl '60s"));
        if (!string.IsNullOrEmpty(sixtiesPlaylist.Id))
        {
            string[] trackUris = tracks60sAndEarlier.Select(u => $"spotify:track:{u.Id}").ToArray();
            const int chunkSize = 100;
            var chunks = trackUris.Batch(chunkSize);
            foreach (var chunk in chunks)
            {
                var snapshot = await spotify.Playlists.AddItems(sixtiesPlaylist.Id, new PlaylistAddItemsRequest(chunk.ToArray()));
            }
        }

        var tracks70s = sortedTracks.Where(t => t.Year is >= 1970 and <= 1979).ToArray();
        Shuffle(tracks70s);
        var seventiesPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest("Classic Vinyl '70s"));
        if (!string.IsNullOrEmpty(seventiesPlaylist.Id))
        {
            string[] trackUris = tracks70s.Select(u => $"spotify:track:{u.Id}").ToArray();
            const int chunkSize = 100;
            var chunks = trackUris.Batch(chunkSize);
            foreach (var chunk in chunks)
            {
                var snapshot = await spotify.Playlists.AddItems(seventiesPlaylist.Id, new PlaylistAddItemsRequest(chunk.ToArray()));
            }
        }

        var tracks80sAndLater = sortedTracks.Where(t => t.Year >= 1980).ToArray();
        Shuffle(tracks80sAndLater);
        var eightiesPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest("Classic Vinyl '80s"));
        if (!string.IsNullOrEmpty(eightiesPlaylist.Id))
        {
            string[] trackUris = tracks80sAndLater.Select(u => $"spotify:track:{u.Id}").ToArray();
            await spotify.Playlists.AddItems(eightiesPlaylist.Id, new PlaylistAddItemsRequest(trackUris));
        }

        return 0;
    }

    #endregion

    #region Helper Methods

    private static TrackInfo[] ParseImportFile(string file)
    {
        List<TrackInfo> tracks = new List<TrackInfo>();

        int lineCount = 0;
        var regex = new Regex(@"\[([^]]+)\]");
        foreach (var line in File.ReadLines(file))
        {
            lineCount++;
            var matches = regex.Matches(line);
            if (matches.Count != 4)
            {
                Console.WriteLine($"Failed to match regex pattern in file {file} on line {lineCount}");
                continue;
            }

            TrackInfo trackInfo = new TrackInfo();
            trackInfo.Song = matches[0].Groups[1].Value;
            trackInfo.Artist = matches[1].Groups[1].Value;
            
            trackInfo.Id = matches[2].Groups[1].Value;
            if (string.IsNullOrEmpty(trackInfo.Id))
            {
                Console.WriteLine($"Could not parse ID in file {file} on line {lineCount}");
            }

            string yearValue = matches[3].Groups[1].Value;
            if (!int.TryParse(yearValue, out var result))
            {
                Console.WriteLine($"Could not parse year {yearValue} in file {file} on line {lineCount}");
                continue;
            }

            trackInfo.Year = result;
            tracks.Add(trackInfo);
        }

        return tracks.ToArray();
    }

    private static void Shuffle<T>(T[] array)
    {
        Random rng = new Random();
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    #endregion

    #region Helper Classes

    private class TrackInfo
    {
        public string Artist { get; set; }
        public string Id { get; set; }
        public string Song { get; set; }
        public int Year { get; set; }

        public override string ToString()
        {
            return $"{Song} by {Artist} ({Year})";
        }
    }

    #endregion

}
