using System.Globalization;

namespace SpeakText.App.Services;

public static class LanguageCodeHelper
{
    public const string AutomaticLanguageCode = "auto";
    private static readonly HashSet<string> SupportedUiLanguageCodes =
    [
        "fr",
        "en",
        "de",
        "es",
        "pt",
        "el",
        "it",
        "hi",
        "ja",
        "zh",
        "ar",
        "ca",
    ];

    public static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return string.Empty;
        }

        return languageCode.Split('-')[0].Trim().ToLowerInvariant();
    }

    public static bool IsAutomaticLanguageCode(string? languageCode)
    {
        return string.Equals(
            NormalizeLanguageCode(languageCode),
            AutomaticLanguageCode,
            StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeUiLanguageCode(string? uiLanguageCode)
    {
        var normalizedCode = NormalizeLanguageCode(uiLanguageCode);
        return SupportsUiLanguageCode(normalizedCode) ? normalizedCode : "en";
    }

    public static bool SupportsUiLanguageCode(string? uiLanguageCode)
    {
        var normalizedCode = NormalizeLanguageCode(uiLanguageCode);
        return !string.IsNullOrWhiteSpace(normalizedCode)
            && (SupportedUiLanguageCodes.Contains(normalizedCode) || TryGetUiCulture(normalizedCode, out _));
    }

    public static string GetPreferredDefaultUiLanguageCode()
    {
        var osLanguageCode = NormalizeLanguageCode(CultureInfo.CurrentUICulture.Name);
        return SupportsUiLanguageCode(osLanguageCode) ? osLanguageCode : "en";
    }

    public static CultureInfo GetUiCulture(string uiLanguageCode)
    {
        return TryGetUiCulture(NormalizeUiLanguageCode(uiLanguageCode), out var culture)
            ? culture
            : CultureInfo.GetCultureInfo("en-US");
    }

    public static string GetLanguageDisplayName(string languageCode, string uiLanguageCode)
    {
        var normalizedCode = NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return string.Empty;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalizedCode);
            var name = NormalizeUiLanguageCode(uiLanguageCode) == "en"
                ? culture.EnglishName
                : culture.NativeName;

            return Capitalize(name, GetUiCulture(uiLanguageCode));
        }
        catch
        {
            return normalizedCode;
        }
    }

    public static string GetDefaultPreviewText(string languageCode)
    {
        var normalizedCode = NormalizeLanguageCode(languageCode);
        return normalizedCode switch
        {
            "fr" => "Bonjour, ici SpeakText. Ceci est un test rapide de la voix s\u00e9lectionn\u00e9e.",
            "en" => "Hello, this is SpeakText. This is a quick preview of the selected voice.",
            "de" => "Hallo, hier ist SpeakText. Dies ist ein kurzer Test der ausgew\u00e4hlten Stimme.",
            "es" => "Hola, aqu\u00ed SpeakText. Esta es una prueba r\u00e1pida de la voz seleccionada.",
            "it" => "Ciao, qui SpeakText. Questa \u00e8 una rapida prova della voce selezionata.",
            "pt" => "Ol\u00e1, aqui \u00e9 o SpeakText. Esta \u00e9 uma amostra r\u00e1pida da voz selecionada.",
            "ar" => "\u0645\u0631\u062d\u0628\u0627\u060c \u0647\u0646\u0627 SpeakText. \u0647\u0630\u0627 \u0627\u062e\u062a\u0628\u0627\u0631 \u0633\u0631\u064a\u0639 \u0644\u0644\u0635\u0648\u062a \u0627\u0644\u0645\u062d\u062f\u062f.",
            "ca" => "Hola, soc SpeakText. Aquesta \u00e9s una prova r\u00e0pida de la veu seleccionada.",
            "el" => "\u0393\u03b5\u03b9\u03b1 \u03c3\u03bf\u03c5, \u03b5\u03b4\u03ce SpeakText. \u0391\u03c5\u03c4\u03ae \u03b5\u03af\u03bd\u03b1\u03b9 \u03bc\u03b9\u03b1 \u03c3\u03cd\u03bd\u03c4\u03bf\u03bc\u03b7 \u03b4\u03bf\u03ba\u03b9\u03bc\u03ae \u03c4\u03b7\u03c2 \u03b5\u03c0\u03b9\u03bb\u03b5\u03b3\u03bc\u03ad\u03bd\u03b7\u03c2 \u03c6\u03c9\u03bd\u03ae\u03c2.",
            "hi" => "\u0928\u092e\u0938\u094d\u0924\u0947, \u092f\u0939\u093e\u0901 SpeakText \u0939\u0948\u0964 \u092f\u0939 \u091a\u0941\u0928\u0940 \u0939\u0941\u0908 \u0906\u0935\u093e\u091c\u093c \u0915\u093e \u090f\u0915 \u091b\u094b\u091f\u093e \u092a\u0930\u0940\u0915\u094d\u0937\u0923 \u0939\u0948\u0964",
            "ja" => "\u3053\u3093\u306b\u3061\u306f\u3001SpeakText\u3067\u3059\u3002\u3053\u308c\u306f\u9078\u629e\u3057\u305f\u97f3\u58f0\u306e\u7c21\u5358\u306a\u30c6\u30b9\u30c8\u3067\u3059\u3002",
            "zh" => "\u4f60\u597d\uff0c\u8fd9\u91cc\u662f SpeakText\u3002\u8fd9\u662f\u6240\u9009\u8bed\u97f3\u7684\u5feb\u901f\u6d4b\u8bd5\u3002",
            _ => BuildFallbackPreviewText(normalizedCode),
        };
    }

    public static string BuildVoicePreviewText(string languageCode, string basePreviewText, string voiceName)
    {
        var normalizedCode = NormalizeLanguageCode(languageCode);
        var previewText = string.IsNullOrWhiteSpace(basePreviewText)
            ? GetDefaultPreviewText(normalizedCode)
            : basePreviewText.Trim();

        if (string.IsNullOrWhiteSpace(voiceName))
        {
            return previewText;
        }

        var voiceSentence = normalizedCode switch
        {
            "fr" => $"Ici, c'est {voiceName}.",
            "en" => $"This is {voiceName}.",
            "de" => $"Hier ist {voiceName}.",
            "es" => $"Aqu\u00ed habla {voiceName}.",
            "pt" => $"Aqui fala {voiceName}.",
            "it" => $"Qui parla {voiceName}.",
            "ca" => $"Aqu\u00ed parla {voiceName}.",
            "ar" => $"\u0647\u0630\u0627 \u0635\u0648\u062a {voiceName}.",
            "el" => $"\u0395\u03B4\u03CE \u03BC\u03B9\u03BB\u03AC\u03B5\u03B9 \u03B7 \u03C6\u03C9\u03BD\u03AE {voiceName}.",
            "hi" => $"\u092F\u0939\u093E\u0901 {voiceName} \u092C\u094B\u0932 \u0930\u0939\u093E \u0939\u0948\u0964",
            "ja" => $"{voiceName} \u3067\u3059\u3002",
            "zh" => $"\u8FD9\u91CC\u662F {voiceName}\u3002",
            _ => $"This is {voiceName}.",
        };

        return string.Concat(voiceSentence, " ", previewText).Trim();
    }

    private static bool TryGetUiCulture(string uiLanguageCode, out CultureInfo culture)
    {
        var normalizedCode = NormalizeLanguageCode(uiLanguageCode);
        var cultureCode = normalizedCode switch
        {
            "en" => "en-US",
            "fr" => "fr-FR",
            "de" => "de-DE",
            "es" => "es-ES",
            "pt" => "pt-PT",
            "el" => "el-GR",
            "it" => "it-IT",
            "hi" => "hi-IN",
            "ja" => "ja-JP",
            "zh" => "zh-CN",
            "ar" => "ar-SA",
            "ca" => "ca-ES",
            _ => normalizedCode,
        };

        try
        {
            culture = CultureInfo.GetCultureInfo(cultureCode);
            return true;
        }
        catch
        {
            culture = CultureInfo.GetCultureInfo("en-US");
            return false;
        }
    }

    private static string Capitalize(string value, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return value.ToUpper(culture);
        }

        return char.ToUpper(value[0], culture) + value[1..];
    }

    private static string BuildFallbackPreviewText(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return "Hello, this is SpeakText. This is a quick preview of the selected voice.";
        }

        try
        {
            var culture = GetUiCulture(languageCode);
            var nativeName = Capitalize(culture.NativeName, culture);
            return $"{nativeName}. SpeakText.";
        }
        catch
        {
            return "Hello, this is SpeakText. This is a quick preview of the selected voice.";
        }
    }
}
