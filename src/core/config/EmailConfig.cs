namespace core.config;

public class EmailConfig
{

    /// <summary>Off until you fill in the SMTP details below.</summary>
    public bool Enabled { get; set; }

    /// <summary>SMTP host. Defaults to Proton Mail Bridge's loopback listener.</summary>
    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 1025;

    /// <summary>
    /// Transport security: "None", "StartTls", "StartTlsWhenAvailable" or "SslOnConnect".
    /// Proton Bridge wants StartTls on 1025; Gmail wants StartTls on 587; most providers offer
    /// SslOnConnect on 465.
    /// </summary>
    public string SecurityMode { get; set; } = "StartTls";

    /// <summary>
    /// Trust a self-signed SMTP certificate. Proton Mail Bridge presents one, so this is needed
    /// for a loopback Bridge setup. Leave it off for anything reached over a network - it disables
    /// the check that would otherwise catch an intercepted connection.
    /// </summary>
    public bool AcceptSelfSignedCertificate { get; set; }

    public string From { get; set; }

    public string To { get; set; }

    public string UserName { get; set; }

    /// <summary>
    /// Name of the environment variable holding the SMTP password. The password itself is
    /// deliberately never written to this file - set the variable yourself with, for example:
    /// setx SPOTICLI_SMTP_PASSWORD "your-password"
    /// Leave the variable unset for a server that needs no authentication.
    /// </summary>
    public string PasswordEnvironmentVariable { get; set; } = "SPOTICLI_SMTP_PASSWORD";

    /// <summary>Send a mail even on a run where nothing moved. Off by default.</summary>
    public bool SendWhenUnchanged { get; set; }

    /// <summary>Include the full current top N alongside the changes.</summary>
    public bool IncludeFullList { get; set; } = true;

    /// <summary>True once there is enough here to attempt a send.</summary>
    public bool IsUsable()
    {
        return Enabled
            && !string.IsNullOrWhiteSpace(Host)
            && !string.IsNullOrWhiteSpace(From)
            && !string.IsNullOrWhiteSpace(To);
    }

}
