using core.config;
using core.services;
using SpotifyAPI.Web;

namespace core.monitor;

public class TopMonitorService : ITopMonitorService
{

    #region Constructors

    public TopMonitorService(ApplicationConfig config, ISpotifyService spotifyService, ISnapshotStore store)
    {
        _config = config;
        _spotifyService = spotifyService;
        _store = store;
    }

    #endregion

    #region Variables

    private readonly ApplicationConfig _config;
    private readonly ISpotifyService _spotifyService;
    private readonly ISnapshotStore _store;

    #endregion

    #region Public Methods

    public async Task<MonitorRunResult> RunAsync(bool persist = true, CancellationToken cancellationToken = default)
    {
        var spotify = RequireLoggedInClient();
        var settings = _config.Monitor;
        int limit = Math.Clamp(settings.Limit, 1, 50);
        var ranges = settings.TimeRanges is { Length: > 0 } ? settings.TimeRanges : ["short"];

        await _store.InitializeAsync(cancellationToken);

        var capturedAt = DateTime.UtcNow;
        var diffs = new List<TopDiff>();

        foreach (var range in ranges)
        {
            diffs.Add(await CompareListAsync(spotify, TopEntityType.Artist, range, limit, capturedAt, persist, cancellationToken));
            diffs.Add(await CompareListAsync(spotify, TopEntityType.Track, range, limit, capturedAt, persist, cancellationToken));
        }

        int pruned = 0;
        if (persist && settings.RetentionDays > 0)
        {
            pruned = await _store.PruneAsync(capturedAt.AddDays(-settings.RetentionDays), cancellationToken);
        }

        return new MonitorRunResult
        {
            CapturedAt = capturedAt,
            Diffs = diffs,
            PrunedSnapshots = pruned
        };
    }

    #endregion

    #region Helper Methods

    private async Task<TopDiff> CompareListAsync(
        SpotifyClient spotify,
        TopEntityType entityType,
        string range,
        int limit,
        DateTime capturedAt,
        bool persist,
        CancellationToken cancellationToken)
    {
        var current = new TopSnapshot
        {
            CapturedAt = capturedAt,
            EntityType = entityType,
            TimeRange = range,
            Entries = entityType == TopEntityType.Artist
                ? await FetchArtistsAsync(spotify, range, limit, cancellationToken)
                : await FetchTracksAsync(spotify, range, limit, cancellationToken)
        };

        var previous = await _store.GetLatestAsync(entityType, range, cancellationToken);
        var diff = TopDiffCalculator.Compare(previous, current);

        if (persist)
        {
            await _store.SaveAsync(current, cancellationToken);
        }

        return diff;
    }

    private static async Task<List<TopEntry>> FetchArtistsAsync(SpotifyClient spotify, string range, int limit, CancellationToken cancellationToken)
    {
        var request = new PersonalizationTopRequest { Limit = limit, TimeRangeParam = SpotifyTimeRange.Parse(range) };
        var result = await spotify.Personalization.GetTopArtists(request, cancellationToken);

        int rank = 1;
        return (result.Items ?? [])
            .Select(a => new TopEntry(rank++, a.Id, a.Name, string.Join(", ", a.Genres.Take(2))))
            .ToList();
    }

    private static async Task<List<TopEntry>> FetchTracksAsync(SpotifyClient spotify, string range, int limit, CancellationToken cancellationToken)
    {
        var request = new PersonalizationTopRequest { Limit = limit, TimeRangeParam = SpotifyTimeRange.Parse(range) };
        var result = await spotify.Personalization.GetTopTracks(request, cancellationToken);

        int rank = 1;
        return (result.Items ?? [])
            .Select(t => new TopEntry(rank++, t.Id, t.Name, string.Join(", ", t.Artists.Select(a => a.Name))))
            .ToList();
    }

    /// <summary>
    /// The CLI variant of this check exits the process, which is the wrong behaviour for an
    /// unattended run - a thrown exception lets the caller log it and set an exit code.
    /// </summary>
    private SpotifyClient RequireLoggedInClient()
    {
        if (_spotifyService.Spotify == null || _spotifyService.Config.Authenticator is not AuthorizationCodeAuthenticator)
        {
            throw new InvalidOperationException(
                "No Spotify user login found. Run `sp0 login` as this user before the monitor can read your top lists.");
        }

        return _spotifyService.Spotify;
    }

    #endregion

}
