using System.Globalization;
using System.Resources;

namespace Companion.Core.Localization;

public static class Text
{
    private static readonly ResourceManager Resources = new("Companion.Core.Localization.Strings", typeof(Text).Assembly);
    public static string Language { get; private set; } = "en";
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "fr" ? "fr-FR" : "en-US");

    public static void SetLanguage(string code)
    {
        if (code is not ("en" or "fr")) throw new ArgumentException("Supported languages: en, fr.", nameof(code));
        Language = code;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }

    public static string T(string key, params object?[] arguments)
    {
        var text = Resources.GetString(key, Culture) ?? throw new MissingManifestResourceException($"Missing text resource: {key}");
        return arguments.Length == 0 ? text : string.Format(Culture, text, arguments);
    }
}
