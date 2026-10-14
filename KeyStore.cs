using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PanoramaViewer;

internal enum PanoramaProvider { Mapy, Google }

internal static class KeyStore
{
    // Preserve the existing Mapy.com filename and DPAPI scope.
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PanoramaViewer");

    internal static string Load() => Load(PanoramaProvider.Mapy);
    internal static void Save(string key) => Save(PanoramaProvider.Mapy, key);

    private static string KeyPath(PanoramaProvider provider) => Path.Combine(
        DirectoryPath, provider == PanoramaProvider.Mapy ? "mapy.key" : "google.key");

    internal static string Load(PanoramaProvider provider)
    {
        try
        {
            var path = KeyPath(provider);
            if (!File.Exists(path)) return string.Empty;
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
        }
        catch
        {
            // An absent/unreadable key is handled by the settings dialog.
            // Never write the secret to a log.
            return string.Empty;
        }
    }

    internal static void Save(PanoramaProvider provider, string key)
    {
        var path = KeyPath(provider);
        key = key.Trim();
        if (key.Length == 0)
        {
            if (File.Exists(path)) File.Delete(path);
            return;
        }

        Directory.CreateDirectory(DirectoryPath);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(temporaryPath, encrypted);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
