using core.monitor;

namespace core.test;

public class EmailReportBuilderTests
{

    #region Helper Methods

    private static readonly DateTime Yesterday = new(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Today = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

    private static TopSnapshot Snapshot(DateTime capturedAt, params string[] names)
    {
        return new TopSnapshot
        {
            CapturedAt = capturedAt,
            EntityType = TopEntityType.Artist,
            TimeRange = "short",
            Entries = names
                .Select((name, index) => new TopEntry(index + 1, $"id-{name}", name, string.Empty))
                .ToList()
        };
    }

    private static MonitorRunResult Result(params TopDiff[] diffs)
    {
        return new MonitorRunResult { CapturedAt = Today, Diffs = diffs };
    }

    private static MonitorRunResult SampleRun()
    {
        // "z" entered at #1, "c" left from #3, "a" and "b" each slipped a place.
        return Result(TopDiffCalculator.Compare(
            Snapshot(Yesterday, "a", "b", "c"),
            Snapshot(Today, "z", "a", "b")));
    }

    #endregion

    #region Tests

    [Test]
    public void BuildSubject_LeadsWithTheChangeTotals()
    {
        Assert.That(EmailReportBuilder.BuildSubject(SampleRun()), Does.StartWith("Spotify top lists - 1 new, 1 out, 2 moved"));
    }

    [Test]
    public void BuildSubject_OnFirstRun_SaysBaseline()
    {
        var result = Result(TopDiffCalculator.Compare(null, Snapshot(Today, "a", "b")));

        Assert.That(EmailReportBuilder.BuildSubject(result), Does.Contain("baseline recorded"));
    }

    [Test]
    public void BuildHtml_IncludesEveryChangeAndTheFullCurrentList()
    {
        string html = EmailReportBuilder.BuildHtml(SampleRun());

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("NEW"));
            Assert.That(html, Does.Contain("OUT"));
            Assert.That(html, Does.Contain("Current top 3"));
            // "c" left, so it belongs in the changes table but not the standings.
            Assert.That(html, Does.Contain("new at #1"));
            Assert.That(html, Does.Contain("was #3 of 3"));
        });
    }

    [Test]
    public void BuildHtml_WhenFullListSuppressed_OmitsTheStandings()
    {
        string html = EmailReportBuilder.BuildHtml(SampleRun(), includeFullList: false);

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Not.Contain("Current top"));
            Assert.That(html, Does.Contain("NEW"));
        });
    }

    [Test]
    public void BuildHtml_EscapesNamesSoMarkupCannotLeakIn()
    {
        var previous = new TopSnapshot
        {
            CapturedAt = Yesterday,
            EntityType = TopEntityType.Artist,
            TimeRange = "short",
            Entries = [new TopEntry(1, "id-1", "Safe", string.Empty), new TopEntry(2, "id-2", "Filler", string.Empty)]
        };
        var current = new TopSnapshot
        {
            CapturedAt = Today,
            EntityType = TopEntityType.Artist,
            TimeRange = "short",
            Entries = [new TopEntry(1, "id-x", "<script>alert(1)</script> & W&W", string.Empty)]
        };

        string html = EmailReportBuilder.BuildHtml(Result(TopDiffCalculator.Compare(previous, current)));

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Not.Contain("<script>"));
            Assert.That(html, Does.Contain("&lt;script&gt;"));
            Assert.That(html, Does.Contain("W&amp;W"));
        });
    }

    [Test]
    public void BuildHtml_UsesNoExternalResources()
    {
        string html = EmailReportBuilder.BuildHtml(SampleRun());

        // Mail clients block remote content, so the report must not depend on any.
        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Not.Contain("http://"));
            Assert.That(html, Does.Not.Contain("https://"));
            Assert.That(html, Does.Not.Contain("<img"));
            Assert.That(html, Does.Not.Contain("<link"));
        });
    }

    [Test]
    public void BuildPlainText_IncludesTheChangesAndTheRankedList()
    {
        string text = EmailReportBuilder.BuildPlainText(SampleRun());

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("entered at #1"));
            Assert.That(text, Does.Contain("no longer in the top 3"));
            Assert.That(text, Does.Contain("Current top 3 artists"));
            Assert.That(text, Does.Contain("NEW"));
        });
    }

    [Test]
    public void BuildPlainText_ContainsNoMarkup()
    {
        string text = EmailReportBuilder.BuildPlainText(SampleRun());

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Not.Contain("<td"));
            Assert.That(text, Does.Not.Contain("<span"));
            Assert.That(text, Does.Not.Contain("&mdash;"));
        });
    }

    [Test]
    public void BuildHtml_OnAQuietRun_SaysSoWithoutAChangesTable()
    {
        var diff = TopDiffCalculator.Compare(Snapshot(Yesterday, "a", "b"), Snapshot(Today, "a", "b"));

        string html = EmailReportBuilder.BuildHtml(Result(diff));

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("No movement since the last run."));
            Assert.That(html, Does.Contain("Current top 2"));
        });
    }

    [Test]
    public void BuildHtml_MarksMovementInTheStandings()
    {
        var previous = Snapshot(Yesterday, "a", "b", "c");
        var current = Snapshot(Today, "c", "a", "b");

        string html = EmailReportBuilder.BuildHtml(Result(TopDiffCalculator.Compare(previous, current)));

        // "c" climbed two places; the up arrow and delta should both appear.
        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("&#9650; 2"));
            Assert.That(html, Does.Contain("&#9660; 1"));
        });
    }

    #endregion

}
