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
            "fr" => "Bonjour, ici SpeakText. Ceci est un test rapide de la voix sélectionnée.",
            "en" => "Hello, this is SpeakText. This is a quick preview of the selected voice.",
            "de" => "Hallo, hier ist SpeakText. Dies ist ein kurzer Test der ausgewählten Stimme.",
            "es" => "Hola, aquí SpeakText. Esta es una prueba rápida de la voz seleccionada.",
            "it" => "Ciao, qui SpeakText. Questa è una rapida prova della voce selezionata.",
            "pt" => "Olá, aqui é o SpeakText. Esta é uma amostra rápida da voz selecionada.",
            "ar" => "مرحبا، هنا SpeakText. هذا اختبار سريع للصوت المحدد.",
            "ca" => "Hola, soc SpeakText. Aquesta és una prova ràpida de la veu seleccionada.",
            "el" => "Γεια σου, εδώ SpeakText. Αυτή είναι μια σύντομη δοκιμή της επιλεγμένης φωνής.",
            "hi" => "नमस्ते, यहाँ SpeakText है। यह चुनी हुई आवाज़ का एक छोटा परीक्षण है।",
            "ja" => "こんにちは、SpeakTextです。これは選択した音声の簡単なテストです。",
            "zh" => "你好，这里是 SpeakText。这是所选语音的快速测试。",
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
            "es" => $"Aquí habla {voiceName}.",
            "pt" => $"Aqui fala {voiceName}.",
            "it" => $"Qui parla {voiceName}.",
            "ca" => $"Aquí parla {voiceName}.",
            "ar" => $"هذا صوت {voiceName}.",
            "el" => $"Εδώ μιλάει η φωνή {voiceName}.",
            "hi" => $"यहाँ {voiceName} बोल रहा है।",
            "ja" => $"{voiceName} です。",
            "zh" => $"这里是 {voiceName}。",
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
