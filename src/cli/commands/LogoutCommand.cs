using core;
using core.config;

namespace cli.commands;

public static class LogoutCommand
{

    #region Public Methods

    public static Task<int> Execute(ApplicationConfig config)
    {
        config.SpotifyToken.AccessToken = null;
        config.SpotifyToken.RefreshToken = null;
        config.SpotifyToken.CreatedAt = null;
        config.SpotifyToken.ExpiresIn = null;
        config.SpotifyToken.TokenType = null;
        config.Save();
        ConsoleWrapper.WriteLine("Account has been logged out!", ConsoleColor.Green);
        return Task.FromResult(0);
    }

    #endregion

}
