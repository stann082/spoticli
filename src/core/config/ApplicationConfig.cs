using System.Text;
using Newtonsoft.Json;

namespace core.config;

public class ApplicationConfig
{

    #region Constants

    public const string AppName = "spoticli";

    #endregion

    #region Utility

    public static string AppConfigRootPath = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create
        ),
        AppName
    );

    public static string AppConfigFilePath = Path.Combine(
        AppConfigRootPath,
        "config.json"
    );

    #endregion

    #region Properties

    public AccountConfig Account { get; } = new AccountConfig();
    public SpotifyAppConfig SpotifyApp { get; } = new SpotifyAppConfig();
    public SpotifyTokenConfig SpotifyToken { get; } = new SpotifyTokenConfig();

    #endregion

    #region Public Methods

    public void Save()
    {
        File.WriteAllText(AppConfigFilePath, JsonConvert.SerializeObject(this), Encoding.UTF8);
    }

    public static void Delete()
    {
        File.Delete(AppConfigFilePath);
    }

    public bool HasCredentials()
    {
        return !string.IsNullOrEmpty(SpotifyApp.ClientId) && !string.IsNullOrEmpty(SpotifyApp.ClientSecret);
    }

    public static ApplicationConfig Load()
    {
        Directory.CreateDirectory(AppConfigRootPath);
        if (!File.Exists(AppConfigFilePath))
        {
            var config = new ApplicationConfig();
            config.Save();
            return config;
        }

        var configContent = File.ReadAllText(AppConfigFilePath);
        return JsonConvert.DeserializeObject<ApplicationConfig>(configContent);
    }

    #endregion

}
