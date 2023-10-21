using cli.options;
using core;
using core.config;
using core.services;
using Newtonsoft.Json;

namespace cli.commands;

public static class RestoreCommand
{

    #region Public Methods

    public static async Task<int> Execute(RestoreOptions options, ISpotifyService spotifyService)
    {
        Dictionary<long, string> folderMap = CreateFolderMap();
        if (folderMap == null)
        {
            return 1;
        }

        var allTracks = await GetFavoriteTrackSummaries(folderMap);

        int trackNumber;
        while (true)
        {
            Console.Write("\nChoose the track number you'd like to restore (or press 'e' to exit): ");
            string input = Console.ReadLine();
            if (input?.ToLower() == "e")
            {
                Console.WriteLine("Exiting application.");
                return 1;
            }

            if (int.TryParse(input, out trackNumber) && trackNumber > 0 && trackNumber <= allTracks.Count)
            {
                break;
            }

            ConsoleWrapper.WriteLine("Not a valid selection. Please try again.", ConsoleColor.Red);
        }

        int index = trackNumber - 1;
        FavoriteTrackSummary selectedTrack = allTracks[index];
        Console.WriteLine($"You've selected {selectedTrack.Tracks.GetTrackSummary()} to be restored to {selectedTrack.PlaylistName}");
        
        spotifyService.EnsureUserLoggedIn(out var spotify);
        return 0;
    }

    #endregion

    #region Helper Methods

    private static Dictionary<long, string> CreateFolderMap()
    {
        string folderName = Path.Combine(ApplicationConfig.AppConfigRootPath, "deleted");
        if (!Directory.Exists(folderName))
        {
            Console.WriteLine($"Folder {folderName} does not exist");
            return null;
        }

        Dictionary<long, string> folderMap = new Dictionary<long, string>();
        string[] directories = Directory.GetDirectories(folderName, "*", SearchOption.TopDirectoryOnly);
        foreach (string directory in directories)
        {
            long key = long.TryParse(Path.GetFileName(directory), out long result) ? result : 0;
            if (key == 0)
            {
                ConsoleWrapper.WriteLine($"Could not get the proper unix date from folder {directory}", ConsoleColor.Red);
                continue;
            }

            folderMap[key] = directory;
        }

        return folderMap;
    }

    private static async Task<List<FavoriteTrackSummary>> GetFavoriteTrackSummaries(Dictionary<long, string> folderMap)
    {
        List<FavoriteTrackSummary> allTracks = new List<FavoriteTrackSummary>();

        int count = 1;
        foreach (var pair in folderMap.OrderByDescending(m => m.Key))
        {
            string file = Path.Combine(pair.Value, Constants.FavoritesFileName);
            if (!File.Exists(file))
            {
                ConsoleWrapper.WriteLine($"File {file} does not exist", ConsoleColor.Red);
                continue;
            }

            string fileContents = await File.ReadAllTextAsync(file);
            FavoriteTrackSummary[] tracks = JsonConvert.DeserializeObject<FavoriteTrackSummary[]>(fileContents);
            allTracks.AddRange(tracks);

            ConsoleWrapper.WriteLine($"\nTracks deleted on {pair.Key.FromEpochTime()}", ConsoleColor.Green);
            foreach (FavoriteTrackSummary track in tracks)
            {
                Console.WriteLine($"{count}) {track.Tracks.GetTrackSummary()} from playlist {track.PlaylistName}");
                count++;
            }
        }

        return allTracks;
    }

    #endregion

}
