using System.Globalization;
using System.Text;
using cli.options;
using core;
using core.services;
using SpotifyAPI.Web;

namespace cli.commands;

public static class TracksCommand
{

    #region Public Methods

    public static async Task<int> Execute(TracksOptions options, ISpotifyService spotifyService)
    {
        if (options.Recent > Constants.MaxRecentlyPlayed)
        {
            Console.WriteLine($"The maximum number cannot be greater than {Constants.MaxRecentlyPlayed}");
            return 1;
        }

        spotifyService.EnsureUserLoggedIn(out var spotify);
        var paging = await spotify.Player.GetRecentlyPlayed(new PlayerRecentlyPlayedRequest
        {
            Limit = options.Recent
        });

        PlayHistoryItem[] items = paging.Items != null
            ? paging.Items.ToArray()
            : Array.Empty<PlayHistoryItem>();

        switch (options.Format)
        {
            case "inline":
            {
                InlineResults(items, options.DisplayTime);
                break;
            }
            case "table":
            {
                TableResults(items);
                break;
            }
        }

        return 0;
    }

    #endregion

    #region Helper Methods

    private static string CreateTableFormat(PlayHistoryItem[] items)
    {
        var maxSongNameLength = items.Max(i => i.Track.Name.Length);
        var maxArtistLength = items.Max(i => string.Join(", ", i.Track.Artists.Select(a => a.Name)).Length);
        var maxDateLength = items.Max(i => i.PlayedAt.ToLocalTime().ToString(CultureInfo.InvariantCulture).Length);

        StringBuilder sb = new StringBuilder();
        sb.Append("{0,");
        sb.Append($"-{maxSongNameLength + 3}");
        sb.Append("} ");
        sb.Append("{1,");
        sb.Append($"-{maxArtistLength + 3} ");
        sb.Append("} ");
        sb.Append("{2,");
        sb.Append($"-{maxDateLength}");
        sb.Append('}');
        return sb.ToString();
    }
    
    private static void InlineResults(IEnumerable<PlayHistoryItem> items, bool displayTime)
    {
        foreach (PlayHistoryItem item in items.OrderByDescending(t => t.PlayedAt))
        {
            var artists = item.Track.Artists.Select(a => a.Name);
            Console.Write($"{item.Track.Name} - {string.Join(", ", artists)}");

            if (displayTime)
            {
                Console.Write($" [Played at {item.PlayedAt.ToLocalTime()}]");
            }

            Console.WriteLine();
        }
    }

    private static void TableResults(IEnumerable<PlayHistoryItem> items)
    {
        var playHistoryItems = items.OrderByDescending(t => t.PlayedAt).ToArray();
        string format = CreateTableFormat(playHistoryItems);
        Console.WriteLine(format, "Song", "Artist", "Played at");
        foreach (PlayHistoryItem item in playHistoryItems)
        {
            var artists = item.Track.Artists.Select(a => a.Name);
            Console.WriteLine(format, $"{item.Track.Name}", $"{string.Join(", ", artists)}", $"{item.PlayedAt.ToLocalTime()}");
        }
    }

    #endregion

}
