using core.config;
using SpotifyAPI.Web;

namespace core.services;

public class SpotifyService : ISpotifyService
{

    private readonly ApplicationConfig _appConfig;

    public SpotifyService(ApplicationConfig appConfig)
    {
        _appConfig = appConfig;

        if (!string.IsNullOrEmpty(appConfig.SpotifyToken.RefreshToken))
        {
            // We're logged in as a user
            Config = CreateForUser();
            Spotify = new SpotifyClient(Config);
        }
        else if (
            !string.IsNullOrEmpty(appConfig.SpotifyApp.ClientId)
            && !string.IsNullOrEmpty(appConfig.SpotifyApp.ClientSecret)
        )
        {
            Config = CreateForCredentials();
            Spotify = new SpotifyClient(Config);
        }
        else
        {
            Config = SpotifyClientConfig.CreateDefault();
        }

        OAuth = new OAuthClient(Config);
    }

    private SpotifyClientConfig CreateForUser()
    {
        return SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new AuthorizationCodeAuthenticator(
                _appConfig.SpotifyApp.ClientId!,
                _appConfig.SpotifyApp.ClientSecret!,
                new AuthorizationCodeTokenResponse
                {
                    AccessToken = _appConfig.SpotifyToken.AccessToken!,
                    CreatedAt = (DateTime)_appConfig.SpotifyToken.CreatedAt!,
                    RefreshToken = _appConfig.SpotifyToken.RefreshToken!,
                    ExpiresIn = (int)_appConfig.SpotifyToken.ExpiresIn!,
                    TokenType = _appConfig.SpotifyToken.TokenType!
                }
            ))
            .WithRetryHandler(new SimpleRetryHandler());
    }

    private SpotifyClientConfig CreateForCredentials()
    {
        return SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(
                _appConfig.SpotifyApp.ClientId!,
                _appConfig.SpotifyApp.ClientSecret!,
                string.IsNullOrEmpty(_appConfig.SpotifyToken.AccessToken)
                    ? null
                    : new ClientCredentialsTokenResponse
                    {
                        AccessToken = _appConfig.SpotifyToken.AccessToken!,
                        CreatedAt = (DateTime)_appConfig.SpotifyToken.CreatedAt!,
                        ExpiresIn = (int)_appConfig.SpotifyToken.ExpiresIn!,
                        TokenType = _appConfig.SpotifyToken.TokenType!
                    }
            ))
            .WithRetryHandler(new SimpleRetryHandler());
    }

    public bool EnsureUserLoggedIn(out SpotifyClient spotify)
    {
        if (Spotify == null || Config.Authenticator is not AuthorizationCodeAuthenticator)
        {
            spotify = null;
            ConsoleWrapper.WriteLine("This action requires a user to be logged in - try `sp0 login`", ConsoleColor.Red);
            Environment.Exit(1);
        }

        spotify = Spotify;
        return true;
    }

    public bool EnsureCredentialsSet(out SpotifyClient spotify)
    {
        if (Spotify == null || Config.Authenticator == null)
        {
            spotify = null;
            ConsoleWrapper.WriteLine("This action requires spotify application credentials to be set - try `sp0 config --help`", ConsoleColor.Red);
            Environment.Exit(1);
        }

        spotify = Spotify;
        return true;
    }

    public SpotifyClientConfig Config { get; }
    public SpotifyClient Spotify { get; }
    public OAuthClient OAuth { get; }

}
