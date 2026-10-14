using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace PanoramaViewer;
internal static class KeyStore
{
    private static readonly string PathToKey = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PanoramaViewer", "mapy.key");
    internal static string Load()
    {
        try
        {
            if (!File.Exists(PathToKey)) return "";
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(PathToKey), null, DataProtectionScope.CurrentUser));
        }
        catch { return ""; }
    }
    internal static void Save(string key)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathToKey)!);
        File.WriteAllBytes(PathToKey, ProtectedData.Protect(Encoding.UTF8.GetBytes(key.Trim()), null, DataProtectionScope.CurrentUser));
    }
}
