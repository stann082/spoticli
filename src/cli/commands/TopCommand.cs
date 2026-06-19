using cli.options;
using core.services;
using SpotifyAPI.Web;

namespace cli.commands;

public static class TopCommand
{

    #region Public Methods

    public static async Task<int> Execute(TopOptions options, ISpotifyService spotifyService)
    {
        if (options.Limit is < 1 or > 50)
        {
            Console.WriteLine("Limit must be between 1 and 50.");
            return 1;
        }

        var timeRange = options.Range switch
        {
            "short" => PersonalizationTopRequest.TimeRange.ShortTerm,
            "long" => PersonalizationTopRequest.TimeRange.LongTerm,
            _ => PersonalizationTopRequest.TimeRange.MediumTerm
        };

        spotifyService.EnsureUserLoggedIn(out var spotify);

        bool showBoth = !options.Artists && !options.Tracks;

        if (options.Artists || showBoth)
        {
            await PrintTopArtists(spotify, options.Limit, timeRange);
        }

        if (options.Tracks || showBoth)
        {
            await PrintTopTracks(spotify, options.Limit, timeRange);
        }

        return 0;
    }

    #endregion

    #region Helper Methods

    private static async Task PrintTopArtists(SpotifyClient spotify, int limit, PersonalizationTopRequest.TimeRange timeRange)
    {
        var request = new PersonalizationTopRequest { Limit = limit, TimeRangeParam = timeRange };
        var result = await spotify.Personalization.GetTopArtists(request);

        Console.WriteLine("Top Artists:");
        int rank = 1;
        foreach (var artist in result.Items ?? [])
        {
            var genres = artist.Genres.Count > 0 ? $" [{string.Join(", ", artist.Genres.Take(2))}]" : "";
            Console.WriteLine($"  {rank++,2}. {artist.Name}{genres}");
        }
        Console.WriteLine();
    }

    private static async Task PrintTopTracks(SpotifyClient spotify, int limit, PersonalizationTopRequest.TimeRange timeRange)
    {
        var request = new PersonalizationTopRequest { Limit = limit, TimeRangeParam = timeRange };
        var result = await spotify.Personalization.GetTopTracks(request);

        Console.WriteLine("Top Tracks:");
        int rank = 1;
        foreach (var track in result.Items ?? [])
        {
            var artists = string.Join(", ", track.Artists.Select(a => a.Name));
            Console.WriteLine($"  {rank++,2}. {track.Name} - {artists}");
        }
        Console.WriteLine();
    }

    #endregion

}
