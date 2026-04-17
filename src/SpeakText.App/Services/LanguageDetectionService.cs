using System.Text;
using System.Text.RegularExpressions;

namespace SpeakText.App.Services;

public static partial class LanguageDetectionService
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> StopWordsByLanguage =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["fr"] = new HashSet<string>(
            [
                "le", "la", "les", "un", "une", "des", "de", "du", "et", "est", "dans", "que",
                "qui", "pour", "pas", "avec", "sur", "plus", "au", "aux", "ce", "cette", "ces",
                "il", "elle", "nous", "vous", "je", "tu", "ne", "se", "son", "sa", "ses", "mais",
                "ou", "donc", "car", "bonjour", "merci", "être", "avoir"
            ], StringComparer.OrdinalIgnoreCase),
            ["en"] = new HashSet<string>(
            [
                "the", "a", "an", "and", "is", "are", "this", "that", "these", "those", "with",
                "for", "from", "not", "you", "your", "we", "they", "it", "to", "of", "in", "on",
                "be", "as", "or", "if", "can", "will", "was", "were", "hello", "thanks"
            ], StringComparer.OrdinalIgnoreCase),
            ["de"] = new HashSet<string>(
            [
                "der", "die", "das", "ein", "eine", "und", "ist", "nicht", "mit", "für", "auf",
                "zu", "von", "den", "dem", "dass", "sie", "wir", "ich", "du", "im", "am", "des",
                "als", "auch", "hallo", "danke"
            ], StringComparer.OrdinalIgnoreCase),
            ["es"] = new HashSet<string>(
            [
                "el", "la", "los", "las", "un", "una", "unos", "unas", "y", "es", "con", "para",
                "por", "que", "de", "del", "en", "como", "hola", "gracias"
            ], StringComparer.OrdinalIgnoreCase),
            ["it"] = new HashSet<string>(
            [
                "il", "lo", "la", "gli", "le", "un", "una", "e", "è", "con", "per", "che", "di",
                "del", "della", "nel", "nella", "ciao", "grazie"
            ], StringComparer.OrdinalIgnoreCase),
            ["pt"] = new HashSet<string>(
            [
                "o", "a", "os", "as", "um", "uma", "e", "é", "com", "para", "por", "que", "de",
                "do", "da", "não", "olá", "obrigado", "obrigada"
            ], StringComparer.OrdinalIgnoreCase),
            ["el"] = new HashSet<string>(
            [
                "και", "είναι", "με", "για", "από", "στο", "στη", "το", "η", "ο", "ένα", "μια",
                "γεια", "ευχαριστώ"
            ], StringComparer.OrdinalIgnoreCase),
            ["hi"] = new HashSet<string>(
            [
                "यह", "है", "और", "में", "के", "का", "की", "को", "से", "एक", "मैं", "आप",
                "नहीं", "नमस्ते", "धन्यवाद"
            ], StringComparer.OrdinalIgnoreCase),
        };

    private static readonly IReadOnlyDictionary<string, HashSet<string>> StrongWordsByLanguage =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["fr"] = new HashSet<string>(
            [
                "bonjour", "salut", "merci", "pourquoi", "comment", "oui", "voici", "français"
            ], StringComparer.OrdinalIgnoreCase),
            ["en"] = new HashSet<string>(
            [
                "hello", "thanks", "please", "why", "how", "english", "read", "stop"
            ], StringComparer.OrdinalIgnoreCase),
            ["de"] = new HashSet<string>(
            [
                "hallo", "danke", "bitte", "warum", "deutsch"
            ], StringComparer.OrdinalIgnoreCase),
            ["es"] = new HashSet<string>(
            [
                "hola", "gracias", "favor", "porqué", "español"
            ], StringComparer.OrdinalIgnoreCase),
            ["it"] = new HashSet<string>(
            [
                "ciao", "grazie", "perché", "italiano"
            ], StringComparer.OrdinalIgnoreCase),
            ["pt"] = new HashSet<string>(
            [
                "olá", "obrigado", "obrigada", "português"
            ], StringComparer.OrdinalIgnoreCase),
            ["el"] = new HashSet<string>(
            [
                "γεια", "ευχαριστώ", "παρακαλώ", "ελληνικά"
            ], StringComparer.OrdinalIgnoreCase),
            ["hi"] = new HashSet<string>(
            [
                "नमस्ते", "धन्यवाद", "कृपया", "हिंदी"
            ], StringComparer.OrdinalIgnoreCase),
            ["ja"] = new HashSet<string>(
            [
                "こんにちは", "ありがとう", "日本語"
            ], StringComparer.OrdinalIgnoreCase),
            ["zh"] = new HashSet<string>(
            [
                "你好", "谢谢", "中文", "普通话"
            ], StringComparer.OrdinalIgnoreCase),
        };

    private static readonly IReadOnlyDictionary<string, string[]> RawMarkersByLanguage =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["fr"] =
            [
                " l'", " d'", " qu'", " j'", " c'", " n'", " s'", " t'", " m'",
                " l", " d", " qu", " j", " c", " n", " s", " t", " m",
            ],
            ["en"] =
            [
                "n't", "'re", "'ve", "'ll", "'d", "'m",
                "t", "re", "ve", "ll", "d", "m",
            ],
            ["ja"] =
            [
                "こんにちは", "ありがとう", "です", "ます", "でした", "ません", "日本語"
            ],
            ["zh"] =
            [
                "你好", "谢谢", "中文", "普通话", "的", "了", "是", "在", "我", "你"
            ],
        };

    private static readonly IReadOnlyDictionary<string, string[]> TokenMarkersByLanguage =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["fr"] =
            [
                " est ce ", " s il ", " s il vous plaît ", " je ", " tu ", " vous ", " nous ",
                " bonjour ", " salut ", " merci ", " avec ", " pour "
            ],
            ["en"] =
            [
                " hello ", " thank you ", " thanks ", " please ", " this ", " that ", " with ",
                " read ", " stop ", " why ", " how "
            ],
            ["de"] =
            [
                " hallo ", " danke ", " bitte ", " nicht ", " mit ", " warum "
            ],
            ["es"] =
            [
                " hola ", " gracias ", " por favor ", " que ", " con ", " para "
            ],
            ["it"] =
            [
                " ciao ", " grazie ", " per favore ", " che ", " con ", " per "
            ],
            ["pt"] =
            [
                " olá ", " obrigado ", " obrigada ", " por favor ", " com ", " para "
            ],
            ["el"] =
            [
                " γεια ", " ευχαριστώ ", " παρακαλώ ", " και ", " είναι ", " με ", " για "
            ],
            ["hi"] =
            [
                " नमस्ते ", " धन्यवाद ", " कृपया ", " यह ", " है ", " और ", " में "
            ],
        };

    public static string DetectBestLanguageCode(
        string text,
        IEnumerable<string> candidateLanguageCodes,
        string fallbackLanguageCode)
    {
        var candidates = candidateLanguageCodes
            .Select(LanguageCodeHelper.NormalizeLanguageCode)
            .Where(code => !string.IsNullOrWhiteSpace(code) && !LanguageCodeHelper.IsAutomaticLanguageCode(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
        {
            return NormalizeFallbackLanguageCode(fallbackLanguageCode);
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return ResolveFallbackLanguageCode(candidates, fallbackLanguageCode);
        }

        var normalizedText = text.Normalize(NormalizationForm.FormKC);
        if (ContainsHangul(normalizedText) && candidates.Contains("ko", StringComparer.OrdinalIgnoreCase))
        {
            return "ko";
        }

        if (ContainsArabic(normalizedText) && candidates.Contains("ar", StringComparer.OrdinalIgnoreCase))
        {
            return "ar";
        }

        if (ContainsHebrew(normalizedText) && candidates.Contains("he", StringComparer.OrdinalIgnoreCase))
        {
            return "he";
        }

        if (ContainsGreek(normalizedText) && candidates.Contains("el", StringComparer.OrdinalIgnoreCase))
        {
            return "el";
        }

        if (ContainsDevanagari(normalizedText) && candidates.Contains("hi", StringComparer.OrdinalIgnoreCase))
        {
            return "hi";
        }

        if (ContainsKana(normalizedText) && candidates.Contains("ja", StringComparer.OrdinalIgnoreCase))
        {
            return "ja";
        }

        if (ContainsThai(normalizedText) && candidates.Contains("th", StringComparer.OrdinalIgnoreCase))
        {
            return "th";
        }

        if (ContainsTamil(normalizedText) && candidates.Contains("ta", StringComparer.OrdinalIgnoreCase))
        {
            return "ta";
        }

        if (ContainsCjk(normalizedText))
        {
            if (candidates.Contains("zh", StringComparer.OrdinalIgnoreCase))
            {
                return "zh";
            }

            if (candidates.Contains("ja", StringComparer.OrdinalIgnoreCase))
            {
                return "ja";
            }
        }

        var lowerText = normalizedText.ToLowerInvariant();
        var words = WordRegex()
            .Matches(lowerText)
            .Select(match => match.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (words.Count == 0)
        {
            return ResolveFallbackLanguageCode(candidates, fallbackLanguageCode);
        }

        var joinedWords = $" {string.Join(' ', words)} ";
        var isShortText = words.Count <= 4 || joinedWords.Length <= 28;
        var scores = candidates.ToDictionary(code => code, _ => 0.0, StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            scores[candidate] = ScoreLanguage(lowerText, joinedWords, words, candidate, isShortText);
        }

        var orderedScores = scores
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var bestMatch = orderedScores[0];
        var secondBestScore = orderedScores.Count > 1 ? orderedScores[1].Value : 0.0;

        if (bestMatch.Value <= 0.0)
        {
            return ResolveFallbackLanguageCode(candidates, fallbackLanguageCode);
        }

        var minimumGap = isShortText ? 0.85 : 0.35;
        var minimumWinningScore = isShortText ? 1.60 : 0.80;
        if (bestMatch.Value < minimumWinningScore || (bestMatch.Value - secondBestScore) < minimumGap)
        {
            return ResolveFallbackLanguageCode(candidates, fallbackLanguageCode);
        }

        return bestMatch.Key;
    }

    private static double ScoreLanguage(
        string lowerText,
        string joinedWords,
        IReadOnlyList<string> words,
        string languageCode,
        bool isShortText)
    {
        var score = 0.0;
        score += ScoreStopWords(words, languageCode, isShortText);
        score += ScoreStrongWords(words, languageCode, isShortText);
        score += ScoreRawMarkers(lowerText, languageCode, isShortText);
        score += ScoreTokenMarkers(joinedWords, languageCode, isShortText);
        score += ScoreCharacterMarkers(lowerText, languageCode, isShortText);
        score += ScoreDisambiguationMarkers(lowerText, joinedWords, languageCode, isShortText);
        return score;
    }

    private static double ScoreStopWords(IReadOnlyList<string> words, string languageCode, bool isShortText)
    {
        if (!StopWordsByLanguage.TryGetValue(languageCode, out var stopWords))
        {
            stopWords = GetSupplementalStopWords(languageCode);
            if (stopWords is null)
            {
                return 0.0;
            }
        }

        var score = 0.0;
        foreach (var word in words)
        {
            if (!stopWords.Contains(word))
            {
                continue;
            }

            score += word.Length <= 2 ? 0.55 : 0.95;
            if (isShortText && word.Length >= 3)
            {
                score += 0.20;
            }
        }

        return score;
    }

    private static double ScoreStrongWords(IReadOnlyList<string> words, string languageCode, bool isShortText)
    {
        if (!StrongWordsByLanguage.TryGetValue(languageCode, out var strongWords))
        {
            strongWords = GetSupplementalStrongWords(languageCode);
            if (strongWords is null)
            {
                return 0.0;
            }
        }

        var score = 0.0;
        foreach (var word in words)
        {
            if (!strongWords.Contains(word))
            {
                continue;
            }

            score += isShortText ? 1.80 : 1.30;
        }

        return score;
    }

    private static double ScoreRawMarkers(string lowerText, string languageCode, bool isShortText)
    {
        if (!RawMarkersByLanguage.TryGetValue(languageCode, out var markers))
        {
            return 0.0;
        }

        return CountAny(lowerText, markers) * (isShortText ? 1.55 : 1.20);
    }

    private static double ScoreTokenMarkers(string joinedWords, string languageCode, bool isShortText)
    {
        if (!TokenMarkersByLanguage.TryGetValue(languageCode, out var markers))
        {
            markers = GetSupplementalTokenMarkers(languageCode);
            if (markers is null)
            {
                return 0.0;
            }
        }

        return CountAny(joinedWords, markers) * (isShortText ? 1.35 : 1.00);
    }

    private static double ScoreCharacterMarkers(string lowerText, string languageCode, bool isShortText)
    {
        var multiplier = isShortText ? 1.20 : 1.00;

        return languageCode switch
        {
            "fr" => (CountAny(lowerText, ["à", "â", "ç", "é", "è", "ê", "ë", "î", "ï", "ô", "ù", "û", "ü", "œ", "æ"]) * 0.90 * multiplier),
            "en" => (CountAny(lowerText, ["th", "ing", "you", "this", "that"]) * 0.20 * multiplier),
            "de" => (CountAny(lowerText, ["ä", "ö", "ü", "ß"]) * 1.80 * multiplier),
            "es" => (CountAny(lowerText, ["¿", "¡", "ñ"]) * 2.00 * multiplier),
            "it" => (CountAny(lowerText, ["gli", "che", "zione"]) * 0.45 * multiplier),
            "pt" => (CountAny(lowerText, ["ã", "õ", "ção", "ções"]) * 1.50 * multiplier),
            "el" => (CountAny(lowerText, ["αι", "ει", "οι"]) * 0.35 * multiplier),
            "hi" => (CountAny(lowerText, ["है", "और", "नहीं"]) * 0.50 * multiplier),
            _ => GetSupplementalCharacterScore(lowerText, languageCode, multiplier),
        };
    }

    private static int CountAny(string text, IEnumerable<string> markers)
    {
        var count = 0;
        foreach (var marker in markers)
        {
            var index = 0;
            while ((index = text.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += marker.Length;
            }
        }

        return count;
    }

    private static string ResolveFallbackLanguageCode(IReadOnlyList<string> candidates, string fallbackLanguageCode)
    {
        var normalizedFallback = NormalizeFallbackLanguageCode(fallbackLanguageCode);
        if (candidates.Contains(normalizedFallback, StringComparer.OrdinalIgnoreCase))
        {
            return normalizedFallback;
        }

        return candidates[0];
    }

    private static string NormalizeFallbackLanguageCode(string fallbackLanguageCode)
    {
        var normalizedFallback = LanguageCodeHelper.NormalizeLanguageCode(fallbackLanguageCode);
        return string.IsNullOrWhiteSpace(normalizedFallback) ? "en" : normalizedFallback;
    }

    private static bool ContainsHangul(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\uAC00' and <= '\uD7AF')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsKana(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u3040' and <= '\u30FF')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsGreek(string text)
    {
        foreach (var character in text)
        {
            if ((character is >= '\u0370' and <= '\u03FF')
                || (character is >= '\u1F00' and <= '\u1FFF'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsDevanagari(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u0900' and <= '\u097F')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsCjk(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u4E00' and <= '\u9FFF')
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"[\p{L}\p{M}]+(?:['\u2019-][\p{L}\p{M}]+)?")]
    private static partial Regex WordRegex();
}
