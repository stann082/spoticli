using core;
using SpotifyAPI.Web;

namespace cli.commands.sandbox;

public class SandboxHelper
{

    #region Public Methods

    public async Task<int> DoStuff(ISpotifyClient spotify, IEnumerable<FullTrack> fullTracks)
    {
        var me = await spotify.UserProfile.Current();
        TrackInfo[] tracks = fullTracks.Select(t => new TrackInfo(t)).ToArray();
        TrackInfo[] sortedTracks = tracks.OrderBy(t => t.Year).ToArray();

        var tracks70sAndEarlier = sortedTracks.Where(t => t.Year <= 1979).ToArray();
        Shuffle(tracks70sAndEarlier);
        var seventiesPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest("Classic Hard N' Heavy '70s"));
        if (!string.IsNullOrEmpty(seventiesPlaylist.Id))
        {
            string[] trackUris = tracks70sAndEarlier.Select(u => $"spotify:track:{u.Id}").ToArray();
            const int chunkSize = 100;
            var chunks = trackUris.Batch(chunkSize);
            foreach (var chunk in chunks)
            {
                var snapshot = await spotify.Playlists.AddItems(seventiesPlaylist.Id, new PlaylistAddItemsRequest(chunk.ToArray()));
            }
        }

        var tracks80s = sortedTracks.Where(t => t.Year is >= 1980 and <= 1989).ToArray();
        Shuffle(tracks80s);
        var eightiesPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest("Classic Hard N' Heavy '80s"));
        if (!string.IsNullOrEmpty(eightiesPlaylist.Id))
        {
            string[] trackUris = tracks80s.Select(u => $"spotify:track:{u.Id}").ToArray();
            const int chunkSize = 100;
            var chunks = trackUris.Batch(chunkSize);
            foreach (var chunk in chunks)
            {
                var snapshot = await spotify.Playlists.AddItems(eightiesPlaylist.Id, new PlaylistAddItemsRequest(chunk.ToArray()));
            }
        }

        var tracks90sAndLater = sortedTracks.Where(t => t.Year >= 1990).ToArray();
        Shuffle(tracks90sAndLater);
        var ninetiesPlaylist = await spotify.Playlists.Create(me.Id, new PlaylistCreateRequest("Classic Hard N' Heavy '90s"));
        if (!string.IsNullOrEmpty(ninetiesPlaylist.Id))
        {
            string[] trackUris = tracks90sAndLater.Select(u => $"spotify:track:{u.Id}").ToArray();
            await spotify.Playlists.AddItems(ninetiesPlaylist.Id, new PlaylistAddItemsRequest(trackUris));
        }

        return 0;
    }

    #endregion

    #region Helper Methods

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
        public TrackInfo(FullTrack fullTrack)
        {
            Artist = string.Join(", ", fullTrack.Artists.Select(a => a.Name));
            Id = fullTrack.Id;
            Song = fullTrack.Name;

            RawDate = fullTrack.Album.ReleaseDate;
            if (int.TryParse(RawDate, out var year) && year > 1900)
            {
                Year = year;
            }
            else if (DateTime.TryParse(RawDate, out var date) && date.Year > 1900)
            {
                Year = date.Year;
            }
            else
            {
                Year = 1900;
            }
        }

        public string Artist { get; set; }
        public string Id { get; set; }
        public string RawDate { get; set; }
        public string Song { get; set; }
        public int Year { get; set; }

        public override string ToString()
        {
            return $"{Song} by {Artist} ({Year})";
        }
    }

    #endregion

}
