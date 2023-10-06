using SpotifyAPI.Web;

namespace core;

public static class DuplicateFinder
{

    #region Public Methods

    public static async Task Find(ISpotifyClient spotify, IEnumerable<SimplePlaylist> playlists, IOptions options)
    {
        Dictionary<string, IList<string>> duplicateMap = new Dictionary<string, IList<string>>();
        List<FullTrack> allPlaylistsTracks = new List<FullTrack>();

        if (options.RunSynchronously)
        {
            ProcessPlaylists(spotify, playlists, duplicateMap, allPlaylistsTracks).Wait();
        }
        else
        {
            IEnumerable<IEnumerable<SimplePlaylist>> batches = playlists.Batch(options.BatchSize);
            var tasks = batches.Select(async batch => { await ProcessPlaylists(spotify, batch, duplicateMap, allPlaylistsTracks); });
            await Task.WhenAll(tasks);
        }

        var ids = allPlaylistsTracks.Where(t => t != null).Select(t => t.Id).ToArray();
        var duplicateTracks = ids
            .GroupBy(u => u)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (!duplicateTracks.Any())
        {
            Console.WriteLine("No duplicate tracks found in any playlists");
            return;
        }

        Console.WriteLine("Duplicate tracks:");
        foreach (string duplicateTrackId in duplicateTracks)
        {
            var track = allPlaylistsTracks.First(t => t.Id == duplicateTrackId);
            var playlistsForTrack = duplicateMap[duplicateTrackId];
            Console.WriteLine($"Track: {track.Name} ({string.Join(", ", track.Artists.Select(a => a.Name))})");
            Console.WriteLine($"Appears in playlists: {string.Join(", ", playlistsForTrack)}");
            Console.WriteLine();
        }
    }

    #endregion

    #region Helper Methods

    private static async Task ProcessPlaylists(
        ISpotifyClient spotify,
        IEnumerable<SimplePlaylist> batch,
        IDictionary<string, IList<string>> duplicateMap,
        List<FullTrack> allPlaylistsTracks)
    {
        foreach (var playlist in batch)
        {
            var page = await spotify.Playlists.GetItems(playlist.Id);
            var playlistTracks = (await spotify.PaginateAll(page)).Select(t => t.Track as FullTrack).ToArray();
            playlistTracks = playlistTracks.Where(t => t != null).ToArray();
            if (!playlistTracks.Any())
            {
                continue;
            }

            foreach (var track in playlistTracks)
            {
                if (!duplicateMap.ContainsKey(track.Id))
                {
                    duplicateMap[track.Id] = new List<string>();
                }

                if (duplicateMap.TryGetValue(track.Id, out IList<string> value))
                {
                    value.Add(playlist.Name);
                }
            }

            allPlaylistsTracks.AddRange(playlistTracks);
        }
    }

    #endregion

}
