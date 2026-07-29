using CommandLine;
using core.config;
using core.monitor;
using core.services;
using Microsoft.Extensions.DependencyInjection;
using monitor;
using Serilog;

var parsed = Parser.Default.ParseArguments<MonitorOptions>(args);
if (parsed.Errors.Any())
{
    return 2;
}

var options = parsed.Value;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(ApplicationConfig.AppConfigRootPath, "logs", "monitor-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    var config = ApplicationConfig.Load();

    var services = new ServiceCollection()
        .AddSingleton(config)
        .AddSingleton<ISpotifyService, SpotifyService>()
        .AddSingleton<ISnapshotStore>(_ => new SqliteSnapshotStore())
        .AddSingleton<ITopMonitorService, TopMonitorService>()
        .AddSingleton<ToastNotifier>()
        .AddSingleton<EmailNotifier>()
        .BuildServiceProvider();

    if (options.TestEmail)
    {
        var email = config.Monitor.Email;
        if (!email.IsUsable())
        {
            // A misconfiguration is a normal thing to hit here, so report it rather than
            // letting it surface as a stack trace.
            Log.Error("Email is not configured. Set Monitor.Email.Enabled, Host, From and To in {Path} first",
                ApplicationConfig.AppConfigFilePath);
            return 1;
        }

        Log.Information("Sending a test email to {To} via {Host}:{Port}", email.To, email.Host, email.Port);
        await services.GetRequiredService<EmailNotifier>().SendTestAsync();
        Log.Information("Test email sent");
        return 0;
    }

    Log.Information(
        "Monitor run started (ranges: {Ranges}, limit: {Limit}, dry run: {DryRun})",
        string.Join(", ", config.Monitor.TimeRanges),
        config.Monitor.Limit,
        options.DryRun);

    var result = await services.GetRequiredService<ITopMonitorService>().RunAsync(!options.DryRun);

    foreach (var line in MonitorReport.BuildLines(result))
    {
        Log.Information("{Line}", line);
    }

    if (result.PrunedSnapshots > 0)
    {
        Log.Information("Pruned {Count} snapshots past the retention window", result.PrunedSnapshots);
    }

    if (options.NoNotify)
    {
        Log.Information("Notification skipped (--no-notify)");
    }
    else if (!result.HasChanges && !result.IsBaseline && !options.NotifyAlways)
    {
        // Nothing moved. A daily toast saying so would only train you to ignore them. The email
        // notifier makes the same call independently, so SendWhenUnchanged can still opt in.
        Log.Information("Toast skipped: nothing changed");
        await BuildNotifier(services, config, includeToast: false).NotifyAsync(result);
    }
    else
    {
        await BuildNotifier(services, config, includeToast: config.Monitor.EnableToast).NotifyAsync(result);
    }

    Log.Information("Monitor run completed");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Monitor run failed");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static INotifier BuildNotifier(IServiceProvider services, ApplicationConfig config, bool includeToast)
{
    var channels = new List<INotifier>();

    if (includeToast)
    {
        channels.Add(services.GetRequiredService<ToastNotifier>());
    }

    // Always handed the result: the notifier decides for itself whether a quiet run is worth an
    // email, which is what makes Email.SendWhenUnchanged independent of the toast.
    if (config.Monitor.Email.Enabled)
    {
        channels.Add(services.GetRequiredService<EmailNotifier>());
    }

    if (channels.Count == 0)
    {
        Log.Warning("No notification channel is enabled - the report is in the log only");
    }

    return new CompositeNotifier(channels);
}
