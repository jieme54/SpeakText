using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SpeakText.App.Models;
using Windows.Media.SpeechSynthesis;

namespace SpeakText.App.Services;

public static partial class VoiceCatalogService
{
    private const string SapiVoicesCategoryId = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices";
    private const string OneCoreVoicesCategoryId = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices";

    internal sealed record InstalledWindowsVoice(
        VoiceInformation VoiceInformation,
        string Id,
        string DisplayName,
        string Description,
        string Language,
        string LanguageCode);

    public static IReadOnlyList<EngineOption> GetOverlayLanguageOptions(string uiLanguageCode)
    {
        var options = new List<EngineOption>
        {
            new(
                LanguageCodeHelper.AutomaticLanguageCode,
                UiTextCatalog.Get(uiLanguageCode).AutomaticLanguageOption),
        };

        options.AddRange(GetInstalledLanguageOptions(uiLanguageCode));
        return options;
    }

    public static IReadOnlyList<EngineOption> GetInterfaceLanguageOptions(string uiLanguageCode)
    {
        var options = GetInstalledLanguageOptions(uiLanguageCode)
            .Select(option => new EngineOption(option.Id, LanguageCodeHelper.GetLanguageDisplayName(option.Id, option.Id)))
            .Concat([new EngineOption("en", LanguageCodeHelper.GetLanguageDisplayName("en", "en"))])
            .DistinctBy(option => LanguageCodeHelper.NormalizeLanguageCode(option.Id), StringComparer.OrdinalIgnoreCase)
            .OrderBy(option => option.Label, StringComparer.Create(LanguageCodeHelper.GetUiCulture(uiLanguageCode), ignoreCase: true))
            .ToList();

        return options;
    }

    public static IReadOnlyList<EngineOption> GetInstalledLanguageOptions(string uiLanguageCode)
    {
        var options = new Dictionary<string, EngineOption>(StringComparer.OrdinalIgnoreCase);
        AddInstalledLanguageOptionsFromCategory(options, OneCoreVoicesCategoryId, uiLanguageCode);
        AddInstalledLanguageOptionsFromCategory(options, SapiVoicesCategoryId, uiLanguageCode);

        if (options.Count == 0)
        {
            options["fr"] = new EngineOption("fr", LanguageCodeHelper.GetLanguageDisplayName("fr", uiLanguageCode));
            options["en"] = new EngineOption("en", LanguageCodeHelper.GetLanguageDisplayName("en", uiLanguageCode));
        }

        return options.Values
            .OrderBy(option => option.Label, StringComparer.Create(LanguageCodeHelper.GetUiCulture(uiLanguageCode), ignoreCase: true))
            .ToList();
    }

    public static IReadOnlyList<EngineOption> GetReadableLanguageOptions(string uiLanguageCode)
    {
        var options = new Dictionary<string, EngineOption>(StringComparer.OrdinalIgnoreCase);

        AddInstalledLanguageOptionsFromCategory(options, SapiVoicesCategoryId, uiLanguageCode);

        if (options.Count == 0)
        {
            options["fr"] = new EngineOption("fr", LanguageCodeHelper.GetLanguageDisplayName("fr", uiLanguageCode));
            options["en"] = new EngineOption("en", LanguageCodeHelper.GetLanguageDisplayName("en", uiLanguageCode));
        }

        return options.Values
            .OrderBy(option => option.Label, StringComparer.Create(LanguageCodeHelper.GetUiCulture(uiLanguageCode), ignoreCase: true))
            .ToList();
    }

    public static IReadOnlyList<EngineOption> GetWindowsVoiceOptions(string languageCode, string uiLanguageCode = "fr")
    {
        var options = new List<EngineOption>
        {
            new(
                string.Empty,
                LanguageCodeHelper.NormalizeUiLanguageCode(uiLanguageCode) == "en"
                    ? "Default Windows voice"
                    : "Voix par défaut de Windows"),
        };

        var seenIdentityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddWindowsVoiceOptionsFromCategory(options, seenIdentityKeys, OneCoreVoicesCategoryId, languageCode);
        AddWindowsVoiceOptionsFromCategory(options, seenIdentityKeys, SapiVoicesCategoryId, languageCode);

        return options;
    }

    public static string GetLanguageDisplayName(string languageCode, string uiLanguageCode)
    {
        return LanguageCodeHelper.GetLanguageDisplayName(languageCode, uiLanguageCode);
    }

    public static string NormalizeAvailableInterfaceLanguageCode(
        string preferredUiLanguageCode,
        IEnumerable<EngineOption> availableLanguageOptions)
    {
        var availableLanguageCodes = availableLanguageOptions
            .Select(option => LanguageCodeHelper.NormalizeLanguageCode(option.Id))
            .Where(LanguageCodeHelper.SupportsUiLanguageCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (availableLanguageCodes.Count == 0)
        {
            return "en";
        }

        var preferredLanguageCode = LanguageCodeHelper.NormalizeUiLanguageCode(preferredUiLanguageCode);
        if (availableLanguageCodes.Contains(preferredLanguageCode, StringComparer.OrdinalIgnoreCase))
        {
            return preferredLanguageCode;
        }

        var osLanguageCode = LanguageCodeHelper.NormalizeLanguageCode(CultureInfo.CurrentUICulture.Name);
        if (availableLanguageCodes.Contains(osLanguageCode, StringComparer.OrdinalIgnoreCase))
        {
            return osLanguageCode;
        }

        if (availableLanguageCodes.Contains("en", StringComparer.OrdinalIgnoreCase))
        {
            return "en";
        }

        return availableLanguageCodes[0];
    }

    public static string GetOverlayVoiceBadgeText(string languageCode, string configuredVoiceName, string uiLanguageCode)
    {
        var normalizedLanguageCode = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalizedLanguageCode))
        {
            return UiTextCatalog.Get(uiLanguageCode).WindowsVoicesBadge;
        }

        var resolvedVoice = ResolveWindowsVoice(configuredVoiceName, normalizedLanguageCode);
        if (resolvedVoice is null)
        {
            return $"{UiTextCatalog.Get(uiLanguageCode).WindowsVoicesBadge} ({FormatLanguageBadge(normalizedLanguageCode)})";
        }

        var compactName = SimplifyVoiceDisplayName(resolvedVoice.DisplayName);
        return $"{compactName} ({FormatLanguageBadge(normalizedLanguageCode)})";
    }

    public static string GetSpokenVoiceName(string rawVoiceName)
    {
        if (string.IsNullOrWhiteSpace(rawVoiceName))
        {
            return string.Empty;
        }

        var simplifiedName = SimplifyVoiceDisplayName(rawVoiceName);
        return string.IsNullOrWhiteSpace(simplifiedName) ? rawVoiceName.Trim() : simplifiedName;
    }

    internal static InstalledWindowsVoice? ResolveWindowsVoice(string configuredVoiceName, string fallbackLanguageCode)
    {
        var installedVoices = GetInstalledWindowsVoices();
        if (installedVoices.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(configuredVoiceName))
        {
            var lookupCandidates = GetVoiceLookupCandidates(configuredVoiceName);
            var matchedVoice = installedVoices.FirstOrDefault(voice => VoiceMatchesConfiguredValue(voice, configuredVoiceName, lookupCandidates));
            if (matchedVoice is not null)
            {
                return matchedVoice;
            }

            return null;
        }

        var normalizedLanguageCode = LanguageCodeHelper.NormalizeLanguageCode(fallbackLanguageCode);
        var languageMatch = installedVoices.FirstOrDefault(voice =>
            string.Equals(voice.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase));
        if (languageMatch is not null)
        {
            return languageMatch;
        }

        return installedVoices[0];
    }

    public static bool TryGetVoiceLanguageCode(dynamic token, out string languageCode)
    {
        if (TryGetVoiceCultureName(token, out string cultureName))
        {
            languageCode = LanguageCodeHelper.NormalizeLanguageCode(cultureName);
            return true;
        }

        languageCode = string.Empty;
        return false;
    }

    public static bool TryGetVoiceCultureName(dynamic token, out string cultureName)
    {
        cultureName = string.Empty;

        try
        {
            var rawAttribute = token.GetAttribute("Language") as string ?? string.Empty;
            foreach (var segment in rawAttribute.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(segment, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var localeId))
                {
                    continue;
                }

                cultureName = CultureInfo.GetCultureInfo(localeId).Name;
                return !string.IsNullOrWhiteSpace(cultureName);
            }
        }
        catch
        {
        }

        return false;
    }

    public static string NormalizeVoiceLookupKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = ExtractLookupFragment(value.Trim());
        var separatorIndex = normalized.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            normalized = normalized[..separatorIndex];
        }

        normalized = normalized
            .Replace("Microsoft", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Desktop", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Natural", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Online", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Multilingual", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();

        normalized = NonAlphanumericRegex().Replace(normalized, string.Empty);
        return normalized;
    }

    private static string SimplifyVoiceDisplayName(string rawVoiceName)
    {
        if (string.IsNullOrWhiteSpace(rawVoiceName))
        {
            return rawVoiceName;
        }

        var normalizedName = rawVoiceName
            .Replace("Microsoft", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Desktop", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Natural", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var separatorIndex = normalizedName.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            normalizedName = normalizedName[..separatorIndex].Trim();
        }

        var parenthesisIndex = normalizedName.IndexOf(" (", StringComparison.Ordinal);
        if (parenthesisIndex >= 0)
        {
            normalizedName = normalizedName[..parenthesisIndex].Trim();
        }

        return string.IsNullOrWhiteSpace(normalizedName) ? rawVoiceName.Trim() : normalizedName;
    }

    private static string FormatLanguageBadge(string languageCode)
    {
        var normalizedCode = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return string.Empty;
        }

        return normalizedCode.Length == 1
            ? normalizedCode.ToUpperInvariant()
            : char.ToUpperInvariant(normalizedCode[0]) + normalizedCode[1..].ToLowerInvariant();
    }

    private static IReadOnlyList<InstalledWindowsVoice> GetInstalledWindowsVoices()
    {
        try
        {
            return SpeechSynthesizer.AllVoices
                .Select(voice => new InstalledWindowsVoice(
                    voice,
                    voice.Id,
                    voice.DisplayName ?? string.Empty,
                    voice.Description ?? string.Empty,
                    voice.Language ?? string.Empty,
                    LanguageCodeHelper.NormalizeLanguageCode(voice.Language)))
                .Where(voice => !string.IsNullOrWhiteSpace(voice.Id)
                    && !string.IsNullOrWhiteSpace(voice.DisplayName)
                    && !string.IsNullOrWhiteSpace(voice.LanguageCode))
                .GroupBy(voice => voice.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool HasModernVoiceMatch(string configuredVoiceName, string languageCode)
    {
        var installedVoices = GetInstalledWindowsVoices();
        if (installedVoices.Count == 0)
        {
            return true;
        }

        var normalizedLanguageCode = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
        var lookupCandidates = GetVoiceLookupCandidates(configuredVoiceName);
        return installedVoices.Any(voice =>
            string.Equals(voice.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase)
            && VoiceMatchesConfiguredValue(voice, configuredVoiceName, lookupCandidates));
    }

    private static bool VoiceMatchesConfiguredValue(
        InstalledWindowsVoice voice,
        string configuredVoiceName,
        IReadOnlySet<string> lookupCandidates)
    {
        if (string.Equals(voice.Id, configuredVoiceName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(voice.DisplayName, configuredVoiceName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(voice.Description, configuredVoiceName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return lookupCandidates.Contains(NormalizeVoiceLookupKey(voice.Id))
            || lookupCandidates.Contains(NormalizeVoiceLookupKey(voice.DisplayName))
            || lookupCandidates.Contains(NormalizeVoiceLookupKey(voice.Description))
            || lookupCandidates.Contains(NormalizeVoiceLookupKey(SimplifyVoiceDisplayName(voice.DisplayName)))
            || lookupCandidates.Contains(NormalizeVoiceLookupKey(SimplifyVoiceDisplayName(voice.Description)));
    }

    private static IReadOnlySet<string> GetVoiceLookupCandidates(string configuredVoiceName)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string value)
        {
            var normalizedValue = NormalizeVoiceLookupKey(value);
            if (!string.IsNullOrWhiteSpace(normalizedValue))
            {
                candidates.Add(normalizedValue);
            }
        }

        AddCandidate(configuredVoiceName);
        AddCandidate(GetSpokenVoiceName(configuredVoiceName));

        var fragment = ExtractLookupFragment(configuredVoiceName);
        AddCandidate(fragment);

        if (!string.IsNullOrWhiteSpace(fragment) && !fragment.Contains(' '))
        {
            foreach (var token in fragment.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                AddCandidate(token);

                if (token.Length > 3
                    && token[^1] is 'M' or 'F'
                    && token[..^1].Any(char.IsLetter))
                {
                    AddCandidate(token[..^1]);
                }
            }
        }

        return candidates;
    }

    private static string ExtractLookupFragment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var fragment = value.Trim();
        if (fragment.Contains('\\') || fragment.Contains('/'))
        {
            fragment = fragment.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? fragment;
        }

        if (!fragment.Contains(' ') && (fragment.Contains('_') || fragment.Contains('-') || fragment.Contains('.')))
        {
            var tokens = fragment.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var bestToken = tokens
                .Reverse()
                .FirstOrDefault(token => token.Count(char.IsLetter) >= 2);

            if (!string.IsNullOrWhiteSpace(bestToken))
            {
                fragment = bestToken;
            }
        }

        if (fragment.Length > 3
            && fragment[^1] is 'M' or 'F'
            && fragment[..^1].Any(char.IsLower))
        {
            fragment = fragment[..^1];
        }

        return fragment;
    }

    private static void AddInstalledLanguageOptionsFromCategory(
        IDictionary<string, EngineOption> options,
        string categoryId,
        string uiLanguageCode)
    {
        object? category = null;

        try
        {
            var categoryType = Type.GetTypeFromProgID("SAPI.SpObjectTokenCategory");
            if (categoryType is null)
            {
                return;
            }

            category = Activator.CreateInstance(categoryType);
            if (category is null)
            {
                return;
            }

            dynamic tokenCategory = category;
            tokenCategory.SetId(categoryId, false);
            dynamic tokens = tokenCategory.EnumerateTokens(string.Empty, string.Empty);
            var count = (int)tokens.Count;

            for (var index = 0; index < count; index++)
            {
                dynamic token = tokens.Item(index);
                var description = token.GetDescription() as string ?? string.Empty;
                var tokenId = token.Id as string ?? string.Empty;
                string languageCode;

                if (!TryGetVoiceLanguageCode(token, out languageCode)
                    && !TryInferLanguageCodeFromDescription(description, out languageCode))
                {
                    continue;
                }

                if (string.Equals(categoryId, SapiVoicesCategoryId, StringComparison.OrdinalIgnoreCase)
                    && !HasModernVoiceMatch(tokenId, languageCode)
                    && !HasModernVoiceMatch(description, languageCode))
                {
                    continue;
                }

                var normalizedCode = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
                if (string.IsNullOrWhiteSpace(normalizedCode) || options.ContainsKey(normalizedCode))
                {
                    continue;
                }

                options[normalizedCode] = new EngineOption(
                    normalizedCode,
                    LanguageCodeHelper.GetLanguageDisplayName(normalizedCode, uiLanguageCode));
            }
        }
        catch
        {
        }
        finally
        {
            if (category is not null && Marshal.IsComObject(category))
            {
                Marshal.FinalReleaseComObject(category);
            }
        }
    }

    private static void AddWindowsVoiceOptionsFromCategory(
        ICollection<EngineOption> options,
        ISet<string> seenIdentityKeys,
        string categoryId,
        string languageCode)
    {
        object? category = null;

        try
        {
            var categoryType = Type.GetTypeFromProgID("SAPI.SpObjectTokenCategory");
            if (categoryType is null)
            {
                return;
            }

            category = Activator.CreateInstance(categoryType);
            if (category is null)
            {
                return;
            }

            dynamic tokenCategory = category;
            tokenCategory.SetId(categoryId, false);
            dynamic tokens = tokenCategory.EnumerateTokens(string.Empty, string.Empty);
            var count = (int)tokens.Count;

            for (var index = 0; index < count; index++)
            {
                dynamic token = tokens.Item(index);
                var description = token.GetDescription() as string ?? string.Empty;
                var tokenId = token.Id as string ?? string.Empty;
                var identityKey = NormalizeVoiceLookupKey(description);

                if (!string.IsNullOrWhiteSpace(description)
                    && !string.IsNullOrWhiteSpace(tokenId)
                    && (!string.Equals(categoryId, SapiVoicesCategoryId, StringComparison.OrdinalIgnoreCase)
                        || HasModernVoiceMatch(tokenId, languageCode)
                        || HasModernVoiceMatch(description, languageCode))
                    && MatchesLanguage(token, description, languageCode)
                    && seenIdentityKeys.Add(identityKey))
                {
                    options.Add(new EngineOption(tokenId, description));
                }
            }
        }
        catch
        {
        }
        finally
        {
            if (category is not null && Marshal.IsComObject(category))
            {
                Marshal.FinalReleaseComObject(category);
            }
        }
    }

    private static bool MatchesLanguage(dynamic token, string description, string languageCode)
    {
        var normalizedLanguage = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
        string tokenLanguageCode;
        if (TryGetVoiceLanguageCode(token, out tokenLanguageCode))
        {
            return string.Equals(tokenLanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase);
        }

        if (TryInferLanguageCodeFromDescription(description, out var inferredLanguageCode))
        {
            return string.Equals(
                LanguageCodeHelper.NormalizeLanguageCode(inferredLanguageCode),
                normalizedLanguage,
                StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool TryInferLanguageCodeFromDescription(string description, out string languageCode)
    {
        var normalizedDescription = description.ToLowerInvariant();
        if (normalizedDescription.Contains("french", StringComparison.Ordinal)
            || normalizedDescription.Contains("franç", StringComparison.Ordinal)
            || normalizedDescription.Contains("franc", StringComparison.Ordinal))
        {
            languageCode = "fr";
            return true;
        }

        if (normalizedDescription.Contains("english", StringComparison.Ordinal))
        {
            languageCode = "en";
            return true;
        }

        if (normalizedDescription.Contains("german", StringComparison.Ordinal)
            || normalizedDescription.Contains("deutsch", StringComparison.Ordinal))
        {
            languageCode = "de";
            return true;
        }

        if (normalizedDescription.Contains("spanish", StringComparison.Ordinal)
            || normalizedDescription.Contains("espa", StringComparison.Ordinal))
        {
            languageCode = "es";
            return true;
        }

        if (normalizedDescription.Contains("italian", StringComparison.Ordinal)
            || normalizedDescription.Contains("italiano", StringComparison.Ordinal))
        {
            languageCode = "it";
            return true;
        }

        if (normalizedDescription.Contains("portuguese", StringComparison.Ordinal)
            || normalizedDescription.Contains("portugu", StringComparison.Ordinal))
        {
            languageCode = "pt";
            return true;
        }

        if (normalizedDescription.Contains("chinese", StringComparison.Ordinal)
            || normalizedDescription.Contains("mandarin", StringComparison.Ordinal)
            || normalizedDescription.Contains("cantonese", StringComparison.Ordinal))
        {
            languageCode = "zh";
            return true;
        }

        if (normalizedDescription.Contains("japanese", StringComparison.Ordinal))
        {
            languageCode = "ja";
            return true;
        }

        if (normalizedDescription.Contains("korean", StringComparison.Ordinal))
        {
            languageCode = "ko";
            return true;
        }

        if (normalizedDescription.Contains("arabic", StringComparison.Ordinal))
        {
            languageCode = "ar";
            return true;
        }

        if (normalizedDescription.Contains("bulgarian", StringComparison.Ordinal))
        {
            languageCode = "bg";
            return true;
        }

        if (normalizedDescription.Contains("catalan", StringComparison.Ordinal))
        {
            languageCode = "ca";
            return true;
        }

        if (normalizedDescription.Contains("croatian", StringComparison.Ordinal))
        {
            languageCode = "hr";
            return true;
        }

        if (normalizedDescription.Contains("czech", StringComparison.Ordinal))
        {
            languageCode = "cs";
            return true;
        }

        if (normalizedDescription.Contains("danish", StringComparison.Ordinal))
        {
            languageCode = "da";
            return true;
        }

        if (normalizedDescription.Contains("dutch", StringComparison.Ordinal))
        {
            languageCode = "nl";
            return true;
        }

        if (normalizedDescription.Contains("finnish", StringComparison.Ordinal))
        {
            languageCode = "fi";
            return true;
        }

        if (normalizedDescription.Contains("hebrew", StringComparison.Ordinal))
        {
            languageCode = "he";
            return true;
        }

        if (normalizedDescription.Contains("hungarian", StringComparison.Ordinal))
        {
            languageCode = "hu";
            return true;
        }

        if (normalizedDescription.Contains("indonesian", StringComparison.Ordinal))
        {
            languageCode = "id";
            return true;
        }

        if (normalizedDescription.Contains("malay", StringComparison.Ordinal))
        {
            languageCode = "ms";
            return true;
        }

        if (normalizedDescription.Contains("norwegian", StringComparison.Ordinal))
        {
            languageCode = "nb";
            return true;
        }

        if (normalizedDescription.Contains("polish", StringComparison.Ordinal))
        {
            languageCode = "pl";
            return true;
        }

        if (normalizedDescription.Contains("romanian", StringComparison.Ordinal))
        {
            languageCode = "ro";
            return true;
        }

        if (normalizedDescription.Contains("russian", StringComparison.Ordinal))
        {
            languageCode = "ru";
            return true;
        }

        if (normalizedDescription.Contains("slovak", StringComparison.Ordinal))
        {
            languageCode = "sk";
            return true;
        }

        if (normalizedDescription.Contains("slovenian", StringComparison.Ordinal))
        {
            languageCode = "sl";
            return true;
        }

        if (normalizedDescription.Contains("swedish", StringComparison.Ordinal))
        {
            languageCode = "sv";
            return true;
        }

        if (normalizedDescription.Contains("tamil", StringComparison.Ordinal))
        {
            languageCode = "ta";
            return true;
        }

        if (normalizedDescription.Contains("thai", StringComparison.Ordinal))
        {
            languageCode = "th";
            return true;
        }

        if (normalizedDescription.Contains("turkish", StringComparison.Ordinal))
        {
            languageCode = "tr";
            return true;
        }

        if (normalizedDescription.Contains("ukrainian", StringComparison.Ordinal))
        {
            languageCode = "uk";
            return true;
        }

        if (normalizedDescription.Contains("vietnamese", StringComparison.Ordinal))
        {
            languageCode = "vi";
            return true;
        }

        languageCode = string.Empty;
        return false;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();
}
