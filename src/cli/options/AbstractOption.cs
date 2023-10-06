using CommandLine;
using core;

namespace cli.options;

public abstract class AbstractOption : IOptions
{
    [Option("batch-size", Default = 10, HelpText = "Specify the batch size.")]
    public int BatchSize { get; set; }

    [Option("do-not-prompt", HelpText = "All deletion actions will not be prompted.")]
    public bool DoNotPrompt { get; set; }

    [Option("dry-run", HelpText = "Shows an action that would be taken by a specific command.")]
    public bool IsDryRun { get; set; }
    
    [Option("run-synchronously", HelpText = "Run async processes synchronously.")]
    public bool RunSynchronously { get; set; }
}
