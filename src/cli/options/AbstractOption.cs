using CommandLine;

namespace cli.options;

public abstract class AbstractOption
{
    [Option("batch-size", Default = 10, HelpText = "Specify the batch size.")]
    public int BatchSize { get; set; }

    [Option("dry-run", HelpText = "Shows an action that would be taken by a specific command.")]
    public bool IsDryRun { get; set; }
}
