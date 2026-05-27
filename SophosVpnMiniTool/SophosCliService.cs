using System.Diagnostics;
using System.Text;

namespace SophosVpnMiniTool;

internal sealed class SophosCliService
{
    public async Task<ProcessResult> EnableAsync(string sccliPath, string connectionName, string username, string passwordWithToken)
    {
        var arguments = new[] { "enable", "-n", connectionName, "-u", username, "-p", passwordWithToken };
        return await RunAsync(sccliPath, arguments);
    }

    public async Task<ProcessResult> DisableAsync(string sccliPath, string connectionName)
    {
        var arguments = new[] { "disable", "-n", connectionName };
        return await RunAsync(sccliPath, arguments);
    }

    public async Task<ProcessResult> ListDetailedAsync(string sccliPath)
    {
        var arguments = new[] { "list", "-d" };
        return await RunAsync(sccliPath, arguments);
    }

    private static async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException("Sophos Connect konnte nicht gestartet werden. Ist Sophos Connect installiert und ist der sccli.exe-Pfad korrekt?", ex);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);
    }
}
