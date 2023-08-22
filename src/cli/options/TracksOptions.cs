using CommandLine;
using core;

namespace cli.options;

[Verb("tracks", HelpText = "Gets information about spotify tracks.")]
public class TracksOptions
{
    
    [Option('r', "recent", Default = Constants.MaxRecentlyPlayed, HelpText = "Number of recently played tracks.")]
    public int Recent { get; set; }
    
    [Option('t', "time", HelpText = "Display timestamps of each track played.")]
    public bool DisplayTime { get; set; }
    
    [Option('f', "format", Default = "inline", HelpText = "Specify format to display the result (e.g.: table, inline).")]
    public string Format { get; set; }
    
}
