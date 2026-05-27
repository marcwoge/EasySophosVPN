using System.Security.Cryptography;
using System.Text;

namespace SophosVpnMiniTool;

internal sealed class PasswordStore
{
    public bool HasPassword => File.Exists(AppPaths.PasswordFilePath);

    public void SavePassword(string password)
    {
        AppPaths.EnsureAppDataDirectory();
        var plainBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            var protectedBytes = ProtectedData.Protect(plainBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(AppPaths.PasswordFilePath, protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    public string LoadPassword()
    {
        var protectedBytes = File.ReadAllBytes(AppPaths.PasswordFilePath);
        byte[]? plainBytes = null;

        try
        {
            plainBytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Das gespeicherte Passwort konnte nicht entschluesselt werden. Bitte speichern Sie das VPN-Passwort neu.", ex);
        }
        finally
        {
            if (plainBytes is not null)
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
    }
}
