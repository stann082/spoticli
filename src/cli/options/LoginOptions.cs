using System.Reflection;
using CommandLine;
using core;
using SpotifyAPI.Web;

namespace cli.options;

[Verb("login", HelpText = "Log into a user account via OAuth2. Only required when accessing user related data.")]
public class LoginOptions
{

    #region Properties

    [Option('a', "address", Default = Constants.DefaultAddress, HelpText = "URI of the webserver used to authenticate. Also needs to be added as redirect uri to your Spotify app.")]
    public string Address { get; set; }

    [Option('p', "port", Default = Constants.DefaultPort, HelpText = "Listen port of the authentication webserver.")]
    public int Port { get; set; }

    [Option('s', "scopes", HelpText = "A comma seperated list of scopes to request.")]
    public string Scopes { get; set; }

    [Option('t', "timeout", Default = Constants.DefaultTimeout, HelpText = "Timeout of command in seconds.")]
    public int Timeout { get; set; }

    [Option("state", HelpText = "State value used for the authentication.")]
    public string State { get; set; }

    [Option("list-scopes", HelpText = "List all available scopes.")]
    public bool ListScopes { get; set; }

    #endregion

    #region Public Methods

    public static string GetAvailableScopes()
    {
        return string.Join(",", GetDefaultScopes());
    }

    public string[] GetScopes()
    {
        return !string.IsNullOrEmpty(Scopes) ? Scopes.Split(',') : GetDefaultScopes();
    }

    #endregion

    #region Helper Methods

    private static string[] GetDefaultScopes()
    {
        var fields = typeof(Scopes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return fields
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => (string)f.GetRawConstantValue()).ToArray();
    }

    #endregion

}
