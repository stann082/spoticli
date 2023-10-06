using SpotifyAPI.Web;

namespace core;

public static class DuplicateFinder
{

    #region Public Methods

    public static async Task<Dictionary<SimplePlaylist, FullTrack[]>> Find(ISpotifyClient spotify, IEnumerable<SimplePlaylist> playlists, int batchSize)
    {
        Dictionary<SimplePlaylist, FullTrack[]> duplicateMap = new Dictionary<SimplePlaylist, FullTrack[]>();

        IEnumerable<IEnumerable<SimplePlaylist>> batches = playlists.Batch(batchSize);
        var tasks = batches.Select(async batch =>
        {
            foreach (var playlist in batch)
            {
                var page = await spotify.Playlists.GetItems(playlist.Id);
                var playlistTracks = (await spotify.PaginateAll(page)).Select(t => t.Track as FullTrack).ToArray();
                if (!playlistTracks.Any())
                {
                    continue;
                }
        
                var ids = playlistTracks.Where(t => t != null).Select(t => t.Id).ToArray();
                var duplicateTracks = ids
                    .GroupBy(u => u)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
        
                if (!duplicateTracks.Any()) continue;
                FullTrack[] value = playlistTracks.Where(t => duplicateTracks.Contains(t.Id)).ToArray();
                duplicateMap[playlist] = value.Distinct(new TrackIdComparer()).ToArray();
            }
        });
        await Task.WhenAll(tasks);
        return duplicateMap;
    }

    #endregion

    #region Helper Classes

    private class TrackIdComparer : IEqualityComparer<FullTrack>
    {
        public bool Equals(FullTrack x, FullTrack y)
        {
            return x?.Id == y?.Id;
        }

        public int GetHashCode(FullTrack obj)
        {
            return obj.Id.GetHashCode();
        }
    }

    #endregion

}
