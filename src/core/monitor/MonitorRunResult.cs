namespace core.monitor;

/// <summary>
/// Everything one pass of the monitor produced: one diff per tracked list.
/// </summary>
public class MonitorRunResult
{

    public DateTime CapturedAt { get; init; }

    public IReadOnlyList<TopDiff> Diffs { get; init; } = [];

    /// <summary>True when every list was a first capture, so there was nothing to compare against.</summary>
    public bool IsBaseline => Diffs.Count > 0 && Diffs.All(d => d.IsBaseline);

    public bool HasChanges => Diffs.Any(d => d.HasChanges);

    /// <summary>Snapshots discarded by retention on this run.</summary>
    public int PrunedSnapshots { get; init; }

}
