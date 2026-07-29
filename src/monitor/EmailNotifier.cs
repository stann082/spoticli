using core.config;
using core.monitor;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Serilog;

namespace monitor;

/// <summary>
/// Emails the full report over SMTP.
///
/// The password is read from an environment variable rather than the config file, so the secret
/// never lands in %APPDATA%\spoticli\config.json alongside the Spotify token.
/// </summary>
public class EmailNotifier : INotifier
{

    #region Constructors

    public EmailNotifier(ApplicationConfig config)
    {
        _settings = config.Monitor.Email;
    }

    #endregion

    #region Variables

    private readonly EmailConfig _settings;

    #endregion

    #region Public Methods

    public async Task NotifyAsync(MonitorRunResult result, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            Log.Debug("Email notification disabled");
            return;
        }

        if (!_settings.IsUsable())
        {
            Log.Warning("Email is enabled but Host, From or To is missing from the monitor config - skipping the email");
            return;
        }

        if (!result.HasChanges && !result.IsBaseline && !_settings.SendWhenUnchanged)
        {
            Log.Information("Email skipped: nothing changed");
            return;
        }

        try
        {
            await SendAsync(result, cancellationToken);
            Log.Information("Emailed the report to {To}", _settings.To);
        }
        catch (SslHandshakeException ex)
        {
            // Easily the most common setup failure: Proton Bridge presents a self-signed
            // certificate, and the fix is not obvious from the raw error.
            Log.Error(ex,
                "SMTP TLS handshake with {Host}:{Port} failed. If this is Proton Mail Bridge or another " +
                "local relay with a self-signed certificate, set Monitor.Email.AcceptSelfSignedCertificate " +
                "to true in config.json",
                _settings.Host, _settings.Port);
        }
        catch (AuthenticationException ex)
        {
            Log.Error(ex,
                "SMTP rejected the credentials for {UserName}. Check the username and that the {Variable} " +
                "environment variable holds the right password - for Gmail it must be an app password, " +
                "and for Proton Bridge the Bridge-generated password rather than your account password",
                _settings.UserName, _settings.PasswordEnvironmentVariable);
        }
        catch (Exception ex)
        {
            // The snapshot is already saved and the report is in the log, so a delivery failure
            // must not fail the run.
            Log.Error(ex, "Could not email the report to {To}", _settings.To);
        }
    }

    /// <summary>
    /// Sends a short message that exercises the exact same connection path as a real report, so
    /// the SMTP setup can be proven without waiting for a run that has changes.
    /// </summary>
    public async Task SendTestAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.IsUsable())
        {
            throw new InvalidOperationException(
                "Email is not configured. Set Monitor.Email.Enabled, Host, From and To in config.json first.");
        }

        var message = NewMessage("spoticli monitor - test message");
        message.Body = new BodyBuilder
        {
            TextBody = "This is a test from the spoticli monitor. If you are reading it, the SMTP settings work.",
            HtmlBody = "<p style=\"font-family:sans-serif;font-size:14px;\">This is a test from the spoticli monitor. " +
                       "If you are reading it, the SMTP settings work.</p>"
        }.ToMessageBody();

        await TransmitAsync(message, cancellationToken);
    }

    #endregion

    #region Helper Methods

    private async Task SendAsync(MonitorRunResult result, CancellationToken cancellationToken)
    {
        var message = NewMessage(EmailReportBuilder.BuildSubject(result));
        message.Body = new BodyBuilder
        {
            // Both parts are supplied so the mail reads correctly in clients set to plain text.
            TextBody = EmailReportBuilder.BuildPlainText(result, _settings.IncludeFullList),
            HtmlBody = EmailReportBuilder.BuildHtml(result, _settings.IncludeFullList)
        }.ToMessageBody();

        await TransmitAsync(message, cancellationToken);
    }

    private MimeMessage NewMessage(string subject)
    {
        var message = new MimeMessage { Subject = subject };
        message.From.Add(MailboxAddress.Parse(_settings.From));

        foreach (var recipient in _settings.To.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        return message;
    }

    private async Task TransmitAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();

        if (_settings.AcceptSelfSignedCertificate)
        {
            if (!IsLoopback(_settings.Host))
            {
                Log.Warning(
                    "AcceptSelfSignedCertificate is on for the non-local host {Host}. That disables the check " +
                    "which would catch an intercepted connection - prefer a properly trusted certificate",
                    _settings.Host);
            }

            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        await client.ConnectAsync(_settings.Host, _settings.Port, ResolveSecurity(), cancellationToken);

        string password = Environment.GetEnvironmentVariable(_settings.PasswordEnvironmentVariable);
        if (!string.IsNullOrEmpty(_settings.UserName) && !string.IsNullOrEmpty(password))
        {
            await client.AuthenticateAsync(_settings.UserName, password, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(_settings.UserName))
        {
            Log.Warning(
                "A username is configured but {Variable} is not set, so the connection is unauthenticated",
                _settings.PasswordEnvironmentVariable);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private SecureSocketOptions ResolveSecurity()
    {
        return _settings.SecurityMode?.Trim().ToLowerInvariant() switch
        {
            "none" => SecureSocketOptions.None,
            "starttls" => SecureSocketOptions.StartTls,
            "starttlswhenavailable" => SecureSocketOptions.StartTlsWhenAvailable,
            "sslonconnect" => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.Auto
        };
    }

    private static bool IsLoopback(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return System.Net.IPAddress.TryParse(host, out var address) && System.Net.IPAddress.IsLoopback(address);
    }

    #endregion

}
