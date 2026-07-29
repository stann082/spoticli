using SpotifyAPI.Web;

namespace core.monitor;

public static class SpotifyTimeRange
{

    /// <summary>
    /// Maps the CLI/config spelling of a time range onto Spotify's enum. Anything unrecognised
    /// falls back to the medium term, matching the `top` command's default.
    /// </summary>
    public static PersonalizationTopRequest.TimeRange Parse(string range)
    {
        return range switch
        {
            "short" => PersonalizationTopRequest.TimeRange.ShortTerm,
            "long" => PersonalizationTopRequest.TimeRange.LongTerm,
            _ => PersonalizationTopRequest.TimeRange.MediumTerm
        };
    }

    /// <summary>A human-readable window, e.g. "the last 4 weeks".</summary>
    public static string Describe(string range)
    {
        return range switch
        {
            "short" => "the last 4 weeks",
            "long" => "all time",
            _ => "the last 6 months"
        };
    }

}
