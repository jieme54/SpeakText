using SpeakText.App.Models;

namespace SpeakText.App.Services;

public static class AutomaticSpeechPlanner
{
    public static AutomaticSpeechPlan BuildPlan(
        string text,
        AppSettings settings,
        IReadOnlyList<EngineOption> installedLanguageOptions,
        string fallbackLanguageCode)
    {
        var availableLanguageCodes = installedLanguageOptions
            .Select(option => LanguageCodeHelper.NormalizeLanguageCode(option.Id))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var normalizedFallback = ResolveFallbackLanguageCode(availableLanguageCodes, fallbackLanguageCode);
        var chunks = SplitIntoChunks(text);
        var segments = new List<AutomaticSpeechSegment>();
        var charCountByLanguage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rollingFallback = normalizedFallback;

        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk))
            {
                AppendWhitespace(segments, chunk);
                continue;
            }

            var resolvedLanguageCode = LanguageDetectionService.DetectBestLanguageCode(
                chunk,
                availableLanguageCodes,
                rollingFallback);

            if (string.IsNullOrWhiteSpace(resolvedLanguageCode))
            {
                resolvedLanguageCode = normalizedFallback;
            }

            rollingFallback = resolvedLanguageCode;
            AddSegment(segments, chunk, resolvedLanguageCode);

            if (!charCountByLanguage.TryAdd(resolvedLanguageCode, CountSpokenCharacters(chunk)))
            {
                charCountByLanguage[resolvedLanguageCode] += CountSpokenCharacters(chunk);
            }
        }

        if (segments.Count == 0)
        {
            var fallbackProfile = settings.GetLanguageProfile(normalizedFallback);
            return new AutomaticSpeechPlan(
                [new SpeechRequest
                {
                    Text = text,
                    SpeedMultiplier = fallbackProfile.SpeedMultiplier,
                    Pitch = fallbackProfile.Pitch,
                    LanguageProfile = fallbackProfile,
                }],
                [normalizedFallback],
                normalizedFallback);
        }

        var requests = segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment.Text))
            .Select(segment =>
            {
                var profile = settings.GetLanguageProfile(segment.LanguageCode);
                return new SpeechRequest
                {
                    Text = segment.Text,
                    SpeedMultiplier = profile.SpeedMultiplier,
                    Pitch = profile.Pitch,
                    LanguageProfile = profile,
                };
            })
            .ToList();

        var languagesInOrder = segments
            .Select(segment => segment.LanguageCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dominantLanguageCode = charCountByLanguage.Count == 0
            ? normalizedFallback
            : charCountByLanguage
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .First()
                .Key;

        return new AutomaticSpeechPlan(requests, languagesInOrder, dominantLanguageCode);
    }

    private static List<string> SplitIntoChunks(string text)
    {
        var chunks = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var character in text)
        {
            current.Append(character);

            if (character is '\r' or '\n' or '.' or '!' or '?' or ';' or ':' or '\u2026')
            {
                FlushCurrentChunk(chunks, current);
            }
        }

        FlushCurrentChunk(chunks, current);
        return chunks;
    }

    private static void FlushCurrentChunk(ICollection<string> chunks, System.Text.StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        chunks.Add(current.ToString());
        current.Clear();
    }

    private static void AppendWhitespace(IList<AutomaticSpeechSegment> segments, string whitespace)
    {
        if (string.IsNullOrEmpty(whitespace))
        {
            return;
        }

        if (segments.Count == 0)
        {
            return;
        }

        var lastIndex = segments.Count - 1;
        segments[lastIndex] = segments[lastIndex] with
        {
            Text = segments[lastIndex].Text + whitespace,
        };
    }

    private static void AddSegment(IList<AutomaticSpeechSegment> segments, string chunk, string languageCode)
    {
        if (segments.Count > 0
            && string.Equals(segments[^1].LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
        {
            segments[^1] = segments[^1] with
            {
                Text = segments[^1].Text + chunk,
            };
            return;
        }

        segments.Add(new AutomaticSpeechSegment(languageCode, chunk));
    }

    private static int CountSpokenCharacters(string text)
    {
        return text.Count(character => !char.IsWhiteSpace(character));
    }

    private static string ResolveFallbackLanguageCode(IReadOnlyList<string> candidates, string fallbackLanguageCode)
    {
        var normalizedFallback = LanguageCodeHelper.NormalizeLanguageCode(fallbackLanguageCode);
        if (!string.IsNullOrWhiteSpace(normalizedFallback)
            && candidates.Contains(normalizedFallback, StringComparer.OrdinalIgnoreCase))
        {
            return normalizedFallback;
        }

        return candidates.FirstOrDefault() ?? "fr";
    }

    private sealed record AutomaticSpeechSegment(string LanguageCode, string Text);
}

public sealed record AutomaticSpeechPlan(
    IReadOnlyList<SpeechRequest> Requests,
    IReadOnlyList<string> LanguagesInOrder,
    string DominantLanguageCode);
