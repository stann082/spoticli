using CommandLine;
using core;

namespace cli.options;

[Verb("restore", HelpText = "Restores deleted tracks.")]
public class RestoreOptions
{
    
    [Option('d', "days", Default = Constants.DefaultDays, HelpText = "Number of days to check.")]
    public int Days { get; set; }
    
}
