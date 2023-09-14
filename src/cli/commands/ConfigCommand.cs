using cli.options;
using core.config;

namespace cli.commands;

public static class ConfigCommand
{

    #region Public Methods

    public static async Task<int> Execute(ConfigOptions options, ApplicationConfig config)
    {
        if (options.Clear)
        {
            Console.WriteLine("The config file has been deleted");
            ApplicationConfig.Delete();
            return 0;
        }

        if (options.UseEnvVars)
        {
            string clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("Please ensure that both env vars SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET are set");
                return 1;
            }

            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
        }
        
        if (!string.IsNullOrEmpty(options.ClientId))
        {
            config.SpotifyApp.ClientId = options.ClientId;
        }

        if (!string.IsNullOrEmpty(options.ClientSecret))
        {
            config.SpotifyApp.ClientSecret = options.ClientSecret;
        }

        config.Save();
        return 0;
    }

    #endregion

}
