using CommandLine;

namespace cli.options;

[Verb("config", HelpText = "Update or delete config values.")]
public class ConfigOptions
{

    #region Properties

    [Option("client-id", SetName = "cli", HelpText = "Sets the client id of your Spotify app.")]
    public string ClientId { get; set; }

    [Option("client-secret", SetName = "cli", HelpText = "Sets the client secret of your Spotify app.")]
    public string ClientSecret { get; set; }

    [Option('e', "env", SetName = "env", HelpText = "Use client id and secret from env vars.")]
    public bool UseEnvVars { get; set; }

    [Option('c', "clear", Required = false, HelpText = "Delete the config file.")]
    public bool Clear { get; set; }

    #endregion

}
