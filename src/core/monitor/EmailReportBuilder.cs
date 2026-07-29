using System.Net;
using System.Text;

namespace core.monitor;

/// <summary>
/// Renders a run result as an email body.
///
/// Everything is inline-styled with table layout because mail clients strip stylesheets and
/// mangle modern CSS, and nothing is fetched from the network so the report renders the same with
/// remote content blocked.
/// </summary>
public static class EmailReportBuilder
{

    #region Constants

    private const string ColourText = "#202124";
    private const string ColourMuted = "#5f6368";
    private const string ColourBorder = "#e0e0e0";
    private const string ColourUp = "#0f7b3f";
    private const string ColourUpBackground = "#e6f4ea";
    private const string ColourDown = "#b3261e";
    private const string ColourDownBackground = "#fce8e6";

    private const string FontStack = "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    #endregion

    #region Public Methods

    public static string BuildSubject(MonitorRunResult result)
    {
        // BuildToastTitle already phrases the totals; reuse it so the two channels agree.
        string headline = MonitorReport.BuildToastTitle(result).Replace("Spotify: ", string.Empty);
        return $"Spotify top lists - {headline} ({result.CapturedAt.ToLocalTime():ddd d MMM})";
    }

    public static string BuildHtml(MonitorRunResult result, bool includeFullList = true)
    {
        var html = new StringBuilder();

        html.Append($"""
            <div style="margin:0;padding:24px 12px;background:#f5f5f5;font-family:{FontStack};">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid {ColourBorder};border-radius:8px;">
            <tr><td style="padding:24px;">
            <h1 style="margin:0 0 4px;font-size:18px;font-weight:600;color:{ColourText};">Your Spotify top lists</h1>
            <p style="margin:0 0 24px;font-size:13px;color:{ColourMuted};">{Encode(Timestamp(result))}</p>
            """);

        foreach (var diff in result.Diffs)
        {
            AppendSection(html, diff, includeFullList);
        }

        html.Append($"""
            <p style="margin:24px 0 0;padding-top:16px;border-top:1px solid {ColourBorder};font-size:12px;color:{ColourMuted};">
            Sent by the spoticli monitor. Snapshots live in history.db; the full log is in the spoticli data folder.
            </p>
            </td></tr></table></div>
            """);

        return html.ToString();
    }

    public static string BuildPlainText(MonitorRunResult result, bool includeFullList = true)
    {
        var text = new StringBuilder();
        text.AppendLine("Your Spotify top lists");
        text.AppendLine(Timestamp(result));
        text.AppendLine();

        foreach (var line in MonitorReport.BuildLines(result))
        {
            text.AppendLine(line);
        }

        if (includeFullList)
        {
            foreach (var diff in result.Diffs.Where(d => d.Current != null))
            {
                text.AppendLine();
                text.AppendLine($"Current top {diff.ListSize} {diff.Label} ({SpotifyTimeRange.Describe(diff.TimeRange)}):");

                var movement = MovementById(diff);
                foreach (var entry in diff.Current.Entries)
                {
                    string marker = movement.TryGetValue(entry.SpotifyId, out var change)
                        ? PlainMarker(change)
                        : "   ";
                    text.AppendLine($"  {entry.Rank,2}. {marker} {entry.Display}");
                }
            }
        }

        text.AppendLine();
        text.AppendLine("Sent by the spoticli monitor.");

        return text.ToString();
    }

    #endregion

    #region Helper Methods

    private static void AppendSection(StringBuilder html, TopDiff diff, bool includeFullList)
    {
        html.Append($"""
            <h2 style="margin:0 0 2px;font-size:15px;font-weight:600;color:{ColourText};">Top {Encode(diff.Label)}</h2>
            <p style="margin:0 0 12px;font-size:12px;color:{ColourMuted};">{Encode(SpotifyTimeRange.Describe(diff.TimeRange))} &middot; {Encode(diff.Summarize().Split(": ", 2)[^1])}</p>
            """);

        if (diff.IsBaseline)
        {
            html.Append($"""
                <p style="margin:0 0 24px;font-size:13px;color:{ColourMuted};">First run - recorded as the baseline. Changes will be reported from the next run.</p>
                """);
        }
        else if (!diff.HasChanges)
        {
            html.Append($"""
                <p style="margin:0 0 24px;font-size:13px;color:{ColourMuted};">No movement since the last run.</p>
                """);
        }
        else
        {
            AppendChangesTable(html, diff);
        }

        if (includeFullList && diff.Current != null)
        {
            AppendFullListTable(html, diff);
        }
    }

    private static void AppendChangesTable(StringBuilder html, TopDiff diff)
    {
        html.Append("""
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="margin:0 0 20px;border-collapse:collapse;">
            """);

        foreach (var change in diff.Changes)
        {
            html.Append($"""
                <tr>
                <td style="padding:6px 8px 6px 0;border-bottom:1px solid {ColourBorder};width:56px;vertical-align:top;">{Badge(change)}</td>
                <td style="padding:6px 0;border-bottom:1px solid {ColourBorder};font-size:13px;color:{ColourText};">{Encode(change.Display)}</td>
                <td style="padding:6px 0 6px 8px;border-bottom:1px solid {ColourBorder};font-size:12px;color:{ColourMuted};text-align:right;white-space:nowrap;">{Encode(Position(change, diff.ListSize))}</td>
                </tr>
                """);
        }

        html.Append("</table>");
    }

    private static void AppendFullListTable(StringBuilder html, TopDiff diff)
    {
        html.Append($"""
            <p style="margin:0 0 6px;font-size:12px;font-weight:600;color:{ColourMuted};text-transform:uppercase;letter-spacing:0.4px;">Current top {diff.ListSize}</p>
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="margin:0 0 28px;border-collapse:collapse;">
            """);

        var movement = MovementById(diff);

        foreach (var entry in diff.Current.Entries)
        {
            movement.TryGetValue(entry.SpotifyId, out var change);

            html.Append($"""
                <tr>
                <td style="padding:5px 8px 5px 0;border-bottom:1px solid {ColourBorder};width:28px;font-size:12px;color:{ColourMuted};text-align:right;vertical-align:top;">{entry.Rank}</td>
                <td style="padding:5px 0;border-bottom:1px solid {ColourBorder};font-size:13px;color:{ColourText};">{Encode(entry.Display)}</td>
                <td style="padding:5px 0 5px 8px;border-bottom:1px solid {ColourBorder};width:52px;text-align:right;white-space:nowrap;">{Movement(change)}</td>
                </tr>
                """);
        }

        html.Append("</table>");
    }

    /// <summary>
    /// Changes keyed by id for the full-list lookup. Entries that left are skipped because they
    /// are, by definition, not in the current list.
    /// </summary>
    private static Dictionary<string, EntryChange> MovementById(TopDiff diff)
    {
        var movement = new Dictionary<string, EntryChange>(StringComparer.Ordinal);
        foreach (var change in diff.Changes.Where(c => c.Kind != ChangeKind.Left))
        {
            movement.TryAdd(change.SpotifyId, change);
        }

        return movement;
    }

    private static string Badge(EntryChange change)
    {
        return change.Kind switch
        {
            ChangeKind.Entered => Pill("NEW", ColourUp, ColourUpBackground),
            ChangeKind.Left => Pill("OUT", ColourDown, ColourDownBackground),
            ChangeKind.MovedUp => Pill($"&#9650; {change.Delta}", ColourUp, ColourUpBackground),
            ChangeKind.MovedDown => Pill($"&#9660; {Math.Abs(change.Delta)}", ColourDown, ColourDownBackground),
            _ => Pill("&mdash;", ColourMuted, "#f1f3f4")
        };
    }

    private static string Pill(string label, string colour, string background)
    {
        return $"""<span style="display:inline-block;padding:2px 6px;border-radius:3px;background:{background};color:{colour};font-size:11px;font-weight:600;">{label}</span>""";
    }

    private static string Movement(EntryChange change)
    {
        if (change == null)
        {
            return $"""<span style="font-size:12px;color:{ColourMuted};">&mdash;</span>""";
        }

        return change.Kind switch
        {
            ChangeKind.Entered => $"""<span style="font-size:11px;font-weight:600;color:{ColourUp};">NEW</span>""",
            ChangeKind.MovedUp => $"""<span style="font-size:11px;font-weight:600;color:{ColourUp};">&#9650; {change.Delta}</span>""",
            ChangeKind.MovedDown => $"""<span style="font-size:11px;font-weight:600;color:{ColourDown};">&#9660; {Math.Abs(change.Delta)}</span>""",
            _ => $"""<span style="font-size:12px;color:{ColourMuted};">&mdash;</span>"""
        };
    }

    private static string Position(EntryChange change, int listSize)
    {
        return change.Kind switch
        {
            ChangeKind.Entered => $"new at #{change.CurrentRank}",
            ChangeKind.Left => $"was #{change.PreviousRank} of {listSize}",
            _ => $"#{change.PreviousRank} to #{change.CurrentRank}"
        };
    }

    private static string PlainMarker(EntryChange change)
    {
        return change.Kind switch
        {
            ChangeKind.Entered => "NEW",
            ChangeKind.MovedUp => $"+{change.Delta}".PadLeft(3),
            ChangeKind.MovedDown => $"{change.Delta}".PadLeft(3),
            _ => "   "
        };
    }

    private static string Timestamp(MonitorRunResult result)
    {
        return result.CapturedAt.ToLocalTime().ToString("dddd d MMMM yyyy 'at' HH:mm");
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    #endregion

}
