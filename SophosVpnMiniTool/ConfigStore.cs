using System.Text.Json;

namespace SophosVpnMiniTool;

internal sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(AppPaths.ConfigFilePath))
            {
                return new AppConfig();
            }

            var json = File.ReadAllText(AppPaths.ConfigFilePath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        AppPaths.EnsureAppDataDirectory();
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(AppPaths.ConfigFilePath, json);
    }
}
