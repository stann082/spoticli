using CommandLine;

namespace cli.options;

public abstract class AbstractOption
{
    [Option("dry-run", HelpText = "Shows an action that would be taken by a specific command.")]
    public bool IsDryRun { get; set; }
}
