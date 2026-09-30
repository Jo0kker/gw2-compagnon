using System.Text;

namespace Companion.Core.Localization;

/// <summary>Machine preference, deliberately excluded from portable profiles. Applied on restart.</summary>
public static class LanguagePreferences
{
    public static string Load(string path)
    {
        try
        {
            using var file = File.OpenRead(path);
            if (file.Length > 8) return "en";
            using var reader = new StreamReader(file, Encoding.UTF8);
            return reader.ReadToEnd().Trim() is "fr" ? "fr" : "en";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return "en"; }
    }

    public static void Save(string path, string language)
    {
        if (language is not ("en" or "fr")) throw new ArgumentException("Supported languages: en, fr.", nameof(language));
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temp = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { file.Write(Encoding.UTF8.GetBytes(language)); file.Flush(true); }
            File.Move(temp, fullPath, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
