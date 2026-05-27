namespace SophosVpnMiniTool;

internal static class AppPaths
{
    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SophosVpnMiniTool");

    public static string ConfigFilePath => Path.Combine(AppDataDirectory, "config.json");

    public static string PasswordFilePath => Path.Combine(AppDataDirectory, "vpn-password.dpapi");

    public static void EnsureAppDataDirectory()
    {
        Directory.CreateDirectory(AppDataDirectory);
    }
}
