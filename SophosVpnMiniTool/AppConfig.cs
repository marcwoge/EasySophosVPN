namespace SophosVpnMiniTool;

internal sealed class AppConfig
{
    public string SccliPath { get; set; } = @"C:\Program Files (x86)\Sophos\Connect\sccli.exe";

    public string ConnectionName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
}
