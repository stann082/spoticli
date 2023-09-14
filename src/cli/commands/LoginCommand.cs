using cli.options;
using core;
using core.config;
using core.services;
using SpotifyAPI.Web.Auth;

namespace cli.commands;

public static class LoginCommand
{

    #region Public Methods

    public static async Task<int> Execute(LoginOptions options, ApplicationConfig config, ILoginService loginService)
    {
        if (options.ListScopes)
        {
            ConsoleWrapper.WriteLine(LoginOptions.GetAvailableScopes(), ConsoleColor.Cyan);
            return 0;
        }

        if (!config.HasCredentials())
        {
            Console.WriteLine("client-id or client-secret not set. Please run `spoticli config` before logging in");
            return 1;
        }

        var state = !string.IsNullOrEmpty(options.State) ? options.State : Guid.NewGuid().ToString();
        var address = new Uri(options.Address);
        var uri = loginService.GenerateLoginURI(address, config.SpotifyApp.ClientId, options.GetScopes(), state);
        BrowserUtil.Open(uri);

        Console.WriteLine("If no browser opened, visit the following URL manually:\n");
        ConsoleWrapper.WriteLine($"{uri}\n", ConsoleColor.Cyan);

        var loginError = loginService.WaitForLogin(address, options.Port, TimeSpan.FromSeconds(options.Timeout), state).GetAwaiter().GetResult();
        if (loginError == null)
        {
            ConsoleWrapper.WriteLine($"You're now logged in as {config.Account.DisplayName} ({config.Account.Id})", ConsoleColor.Green);
        }
        else
        {
            ConsoleWrapper.WriteLine($"Login failed: {loginError}", ConsoleColor.Red);
        }

        return 0;
    }

    #endregion

}
