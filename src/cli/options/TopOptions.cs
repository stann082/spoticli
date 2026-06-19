using CommandLine;

namespace cli.options;

[Verb("top", HelpText = "Gets your top artists and tracks from Spotify.")]
public class TopOptions
{

    [Option('a', "artists", HelpText = "Show top artists.")]
    public bool Artists { get; set; }

    [Option('t', "tracks", HelpText = "Show top tracks.")]
    public bool Tracks { get; set; }

    [Option('l', "limit", Default = 10, HelpText = "Number of results to return (max 50).")]
    public int Limit { get; set; }

    [Option('r', "range", Default = "medium", HelpText = "Time range: short (4 weeks), medium (6 months), long (all time).")]
    public string Range { get; set; }

}
