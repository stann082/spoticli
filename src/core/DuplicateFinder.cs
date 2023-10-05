using SpotifyAPI.Web;

namespace core;

public class DuplicateFinder
{

    #region Public Methods

    public async Task Find(ISpotifyClient spotify, IEnumerable<SimplePlaylist> playlists, int batchSize)
    {
        Dictionary<string, FullTrack[]> duplicateMap = new Dictionary<string, FullTrack[]>();

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

                var ids = playlistTracks.Select(t => t.Id).ToArray();
                var duplicateTracks = ids
                    // .SelectMany(u => u)
                    .GroupBy(u => u)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (!duplicateTracks.Any()) continue;
                FullTrack[] value = playlistTracks.Where(t => duplicateTracks.Contains(t.Id)).ToArray();
                duplicateMap[playlist.Id] = value;
            }
        });
        await Task.WhenAll(tasks);

        string placeholder = string.Empty;
    }

    #endregion

}
