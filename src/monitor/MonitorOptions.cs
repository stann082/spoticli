using CommandLine;

namespace monitor;

public class MonitorOptions
{

    [Option("dry-run", HelpText = "Fetch and report the diff without writing anything to the history database.")]
    public bool DryRun { get; set; }

    [Option("no-notify", HelpText = "Skip the toast notification; write the report to the console and log only.")]
    public bool NoNotify { get; set; }

    [Option("notify-always", HelpText = "Notify even when nothing changed. By default a no-change run stays quiet.")]
    public bool NotifyAlways { get; set; }

    [Option("test-email", HelpText = "Send a short test email to prove the SMTP settings, then exit without touching Spotify.")]
    public bool TestEmail { get; set; }

}
