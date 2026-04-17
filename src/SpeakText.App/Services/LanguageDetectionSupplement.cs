namespace SpeakText.App.Services;

public static partial class LanguageDetectionService
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> SupplementalStopWordsByLanguage =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ar"] = new HashSet<string>(["و", "في", "من", "على", "مع", "هذا", "هذه", "مرحبا", "شكرا"], StringComparer.OrdinalIgnoreCase),
            ["bg"] = new HashSet<string>(["и", "е", "не", "с", "за", "от", "в", "как", "здравей", "благодаря"], StringComparer.OrdinalIgnoreCase),
            ["ca"] = new HashSet<string>(["el", "la", "els", "les", "un", "una", "i", "es", "amb", "per", "de", "que"], StringComparer.OrdinalIgnoreCase),
            ["cs"] = new HashSet<string>(["a", "je", "neni", "s", "pro", "na", "v", "ze", "to", "ahoj", "dekuji"], StringComparer.OrdinalIgnoreCase),
            ["da"] = new HashSet<string>(["og", "er", "det", "den", "en", "et", "med", "for", "ikke", "hej", "tak"], StringComparer.OrdinalIgnoreCase),
            ["fi"] = new HashSet<string>(["ja", "on", "ei", "se", "etta", "kun", "myos", "hei", "kiitos"], StringComparer.OrdinalIgnoreCase),
            ["he"] = new HashSet<string>(["ו", "של", "עם", "על", "זה", "אני", "אתה", "שלום", "תודה"], StringComparer.OrdinalIgnoreCase),
            ["hr"] = new HashSet<string>(["i", "je", "nije", "s", "za", "na", "u", "da", "bok", "hvala"], StringComparer.OrdinalIgnoreCase),
            ["hu"] = new HashSet<string>(["es", "egy", "nem", "van", "hogy", "a", "az", "ami", "szia", "koszonom"], StringComparer.OrdinalIgnoreCase),
            ["id"] = new HashSet<string>(["dan", "yang", "ini", "itu", "dengan", "untuk", "tidak", "apa", "halo", "terima"], StringComparer.OrdinalIgnoreCase),
            ["ms"] = new HashSet<string>(["dan", "yang", "ini", "itu", "dengan", "untuk", "tidak", "apa", "hai", "terima"], StringComparer.OrdinalIgnoreCase),
            ["nb"] = new HashSet<string>(["og", "er", "det", "den", "en", "et", "med", "for", "ikke", "hei", "takk"], StringComparer.OrdinalIgnoreCase),
            ["nl"] = new HashSet<string>(["de", "het", "een", "en", "is", "met", "voor", "van", "niet", "hallo", "dank"], StringComparer.OrdinalIgnoreCase),
            ["pl"] = new HashSet<string>(["i", "jest", "nie", "z", "na", "do", "ze", "to", "czesc", "dziekuje"], StringComparer.OrdinalIgnoreCase),
            ["ro"] = new HashSet<string>(["si", "este", "nu", "cu", "pentru", "din", "pe", "ce", "salut", "multumesc"], StringComparer.OrdinalIgnoreCase),
            ["ru"] = new HashSet<string>(["и", "в", "не", "на", "с", "что", "это", "как", "привет", "спасибо"], StringComparer.OrdinalIgnoreCase),
            ["sk"] = new HashSet<string>(["a", "je", "nie", "s", "pre", "na", "v", "ze", "ahoj", "dakujem"], StringComparer.OrdinalIgnoreCase),
            ["sl"] = new HashSet<string>(["in", "je", "ni", "z", "za", "na", "v", "da", "zivjo", "hvala"], StringComparer.OrdinalIgnoreCase),
            ["sv"] = new HashSet<string>(["och", "ar", "det", "den", "en", "ett", "med", "for", "inte", "hej", "tack"], StringComparer.OrdinalIgnoreCase),
            ["ta"] = new HashSet<string>(["மற்றும்", "இது", "ஒரு", "உடன்", "க்கு", "இல்", "வணக்கம்", "நன்றி"], StringComparer.OrdinalIgnoreCase),
            ["th"] = new HashSet<string>(["และ", "เป็น", "ใน", "กับ", "ที่", "นี้", "สวัสดี", "ขอบคุณ"], StringComparer.OrdinalIgnoreCase),
            ["tr"] = new HashSet<string>(["ve", "bir", "bu", "su", "ile", "icin", "degil", "ne", "merhaba", "tesekkurler"], StringComparer.OrdinalIgnoreCase),
            ["uk"] = new HashSet<string>(["і", "в", "не", "на", "з", "що", "це", "як", "привіт", "дякую"], StringComparer.OrdinalIgnoreCase),
            ["vi"] = new HashSet<string>(["va", "la", "khong", "cho", "voi", "xin", "chao", "cam", "on"], StringComparer.OrdinalIgnoreCase),
        };

    private static readonly IReadOnlyDictionary<string, HashSet<string>> SupplementalStrongWordsByLanguage =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ar"] = new HashSet<string>(["مرحبا", "شكرا", "العربية"], StringComparer.OrdinalIgnoreCase),
            ["bg"] = new HashSet<string>(["здравей", "благодаря", "български"], StringComparer.OrdinalIgnoreCase),
            ["ca"] = new HashSet<string>(["hola", "gracies", "catala"], StringComparer.OrdinalIgnoreCase),
            ["cs"] = new HashSet<string>(["ahoj", "dekuji", "cesky"], StringComparer.OrdinalIgnoreCase),
            ["da"] = new HashSet<string>(["hej", "tak", "dansk"], StringComparer.OrdinalIgnoreCase),
            ["fi"] = new HashSet<string>(["hei", "kiitos", "suomi"], StringComparer.OrdinalIgnoreCase),
            ["he"] = new HashSet<string>(["שלום", "תודה", "עברית"], StringComparer.OrdinalIgnoreCase),
            ["hr"] = new HashSet<string>(["bok", "hvala", "hrvatski"], StringComparer.OrdinalIgnoreCase),
            ["hu"] = new HashSet<string>(["szia", "koszonom", "magyar"], StringComparer.OrdinalIgnoreCase),
            ["id"] = new HashSet<string>(["halo", "terima", "indonesia"], StringComparer.OrdinalIgnoreCase),
            ["ms"] = new HashSet<string>(["hai", "terima", "melayu"], StringComparer.OrdinalIgnoreCase),
            ["nb"] = new HashSet<string>(["hei", "takk", "norsk"], StringComparer.OrdinalIgnoreCase),
            ["nl"] = new HashSet<string>(["hallo", "dank", "nederlands"], StringComparer.OrdinalIgnoreCase),
            ["pl"] = new HashSet<string>(["czesc", "dziekuje", "polski"], StringComparer.OrdinalIgnoreCase),
            ["ro"] = new HashSet<string>(["salut", "multumesc", "romana"], StringComparer.OrdinalIgnoreCase),
            ["ru"] = new HashSet<string>(["привет", "спасибо", "русский"], StringComparer.OrdinalIgnoreCase),
            ["sk"] = new HashSet<string>(["ahoj", "dakujem", "slovencina"], StringComparer.OrdinalIgnoreCase),
            ["sl"] = new HashSet<string>(["zivjo", "hvala", "slovenscina"], StringComparer.OrdinalIgnoreCase),
            ["sv"] = new HashSet<string>(["hej", "tack", "svenska"], StringComparer.OrdinalIgnoreCase),
            ["ta"] = new HashSet<string>(["வணக்கம்", "நன்றி", "தமிழ்"], StringComparer.OrdinalIgnoreCase),
            ["th"] = new HashSet<string>(["สวัสดี", "ขอบคุณ", "ไทย"], StringComparer.OrdinalIgnoreCase),
            ["tr"] = new HashSet<string>(["merhaba", "tesekkurler", "turkce"], StringComparer.OrdinalIgnoreCase),
            ["uk"] = new HashSet<string>(["привіт", "дякую", "українська"], StringComparer.OrdinalIgnoreCase),
            ["vi"] = new HashSet<string>(["xin", "chao", "viet"], StringComparer.OrdinalIgnoreCase),
        };

    private static readonly IReadOnlyDictionary<string, string[]> SupplementalTokenMarkersByLanguage =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ca"] = [" hola ", " gracies ", " amb ", " per ", " que "],
            ["cs"] = [" ahoj ", " dekuji ", " pro ", " na ", " ze "],
            ["da"] = [" hej ", " tak ", " og ", " med ", " ikke "],
            ["fi"] = [" hei ", " kiitos ", " ja ", " on ", " ei "],
            ["hr"] = [" bok ", " hvala ", " je ", " za ", " u "],
            ["hu"] = [" szia ", " koszonom ", " es ", " egy ", " nem "],
            ["id"] = [" halo ", " terima ", " dan ", " yang ", " ini "],
            ["ms"] = [" hai ", " terima ", " dan ", " yang ", " ini "],
            ["nb"] = [" hei ", " takk ", " og ", " med ", " ikke "],
            ["nl"] = [" hallo ", " dank ", " de ", " het ", " met "],
            ["pl"] = [" czesc ", " dziekuje ", " jest ", " nie ", " na "],
            ["ro"] = [" salut ", " multumesc ", " este ", " nu ", " cu "],
            ["sk"] = [" ahoj ", " dakujem ", " je ", " pre ", " ze "],
            ["sl"] = [" zivjo ", " hvala ", " in ", " je ", " za "],
            ["sv"] = [" hej ", " tack ", " och ", " med ", " inte "],
            ["tr"] = [" merhaba ", " tesekkurler ", " ve ", " bu ", " icin "],
            ["vi"] = [" xin ", " chao ", " va ", " la ", " khong "],
        };

    private static HashSet<string>? GetSupplementalStopWords(string languageCode)
    {
        SupplementalStopWordsByLanguage.TryGetValue(languageCode, out var words);
        return words;
    }

    private static HashSet<string>? GetSupplementalStrongWords(string languageCode)
    {
        SupplementalStrongWordsByLanguage.TryGetValue(languageCode, out var words);
        return words;
    }

    private static string[]? GetSupplementalTokenMarkers(string languageCode)
    {
        SupplementalTokenMarkersByLanguage.TryGetValue(languageCode, out var markers);
        return markers;
    }

    private static double GetSupplementalCharacterScore(string lowerText, string languageCode, double multiplier)
    {
        return languageCode switch
        {
            "ca" => (CountAny(lowerText, ["l·l", "ç", "à", "ò"]) * 1.10 * multiplier),
            "cs" => (CountAny(lowerText, ["ř", "ě", "ů", "č", "ď", "ť"]) * 1.55 * multiplier),
            "da" => (CountAny(lowerText, ["æ", "ø", "å"]) * 1.70 * multiplier),
            "fi" => (CountAny(lowerText, ["ä", "ö"]) * 1.15 * multiplier),
            "hr" => (CountAny(lowerText, ["ć", "đ", "č", "š", "ž"]) * 1.20 * multiplier),
            "nl" => (CountAny(lowerText, ["ij", "lijk"]) * 0.65 * multiplier),
            "nb" => (CountAny(lowerText, ["æ", "ø", "å"]) * 1.45 * multiplier),
            "pl" => (CountAny(lowerText, ["ą", "ę", "ł", "ń", "ó", "ś", "ź", "ż"]) * 1.65 * multiplier),
            "ro" => (CountAny(lowerText, ["ă", "â", "î", "ș", "ț"]) * 1.90 * multiplier),
            "sk" => (CountAny(lowerText, ["ľ", "ĺ", "ô", "ä", "ť", "ď", "ž"]) * 1.55 * multiplier),
            "sl" => (CountAny(lowerText, ["č", "š", "ž"]) * 1.25 * multiplier),
            "sv" => (CountAny(lowerText, ["å", "ä", "ö"]) * 1.65 * multiplier),
            "tr" => (CountAny(lowerText, ["ğ", "ş", "ı", "ç", "ö", "ü"]) * 1.75 * multiplier),
            "vi" => (CountAny(lowerText, ["ă", "â", "ê", "ô", "ơ", "ư", "đ"]) * 1.75 * multiplier),
            _ => 0.0,
        };
    }

    private static double ScoreDisambiguationMarkers(string lowerText, string joinedWords, string languageCode, bool isShortText)
    {
        var multiplier = isShortText ? 1.25 : 1.00;

        return languageCode switch
        {
            "en" => ((CountAny(joinedWords, [" the ", " and ", " with ", " this ", " that ", " you "]) * 0.55)
                + (CountAny(lowerText, ["tion", "ing", "wh"]) * 0.22)) * multiplier,
            "de" => ((CountAny(joinedWords, [" und ", " nicht ", " mit ", " für ", " ich ", " eine ", " der "]) * 0.65)
                + (CountAny(lowerText, ["sch", "ein", "ung"]) * 0.25)) * multiplier,
            "nl" => ((CountAny(joinedWords, [" een ", " het ", " niet ", " met ", " voor ", " jij ", " goed "]) * 0.70)
                + (CountAny(lowerText, ["ij", "lijk", "sch"]) * 0.30)) * multiplier,
            "es" => ((CountAny(joinedWords, [" el ", " la ", " que ", " con ", " por ", " para ", " gracias "]) * 0.45)
                + (CountAny(lowerText, ["ción", "ll", "ñ"]) * 0.30)) * multiplier,
            "pt" => ((CountAny(joinedWords, [" não ", " com ", " para ", " uma ", " você ", " obrigado ", " obrigada "]) * 0.55)
                + (CountAny(lowerText, ["ção", "ções", "nh", "lh"]) * 0.35)) * multiplier,
            "it" => ((CountAny(joinedWords, [" che ", " con ", " per ", " una ", " grazie ", " questo "]) * 0.50)
                + (CountAny(lowerText, ["gli", "zione", "chi"]) * 0.28)) * multiplier,
            "cs" => (CountAny(joinedWords, [" že ", " jsem ", " není ", " pro ", " ahoj ", " děkuji "]) * 0.62) * multiplier,
            "sk" => (CountAny(joinedWords, [" že ", " som ", " nie ", " pre ", " ahoj ", " ďakujem "]) * 0.62) * multiplier,
            "da" => (CountAny(joinedWords, [" og ", " ikke ", " med ", " for ", " hej ", " tak "]) * 0.55) * multiplier,
            "nb" => (CountAny(joinedWords, [" og ", " ikke ", " med ", " for ", " hei ", " takk ", " jeg "]) * 0.58) * multiplier,
            "sv" => (CountAny(joinedWords, [" och ", " inte ", " med ", " för ", " hej ", " tack ", " jag "]) * 0.58) * multiplier,
            "hr" => (CountAny(joinedWords, [" je ", " nije ", " za ", " kako ", " bok ", " hvala "]) * 0.50) * multiplier,
            "sl" => (CountAny(joinedWords, [" je ", " ni ", " za ", " kako ", " živjo ", " hvala "]) * 0.50) * multiplier,
            "id" => (CountAny(joinedWords, [" yang ", " tidak ", " dengan ", " untuk ", " halo ", " terima "]) * 0.52) * multiplier,
            "ms" => (CountAny(joinedWords, [" yang ", " tidak ", " dengan ", " untuk ", " hai ", " anda ", " ialah "]) * 0.55) * multiplier,
            "ro" => (CountAny(joinedWords, [" este ", " nu ", " cu ", " pentru ", " salut ", " mulțumesc ", " multumesc "]) * 0.60) * multiplier,
            "pl" => (CountAny(joinedWords, [" jest ", " nie ", " na ", " cześć ", " dziękuję ", " dziekuje "]) * 0.58) * multiplier,
            "fi" => (CountAny(joinedWords, [" että ", " myös ", " olen ", " hei ", " kiitos "]) * 0.55) * multiplier,
            "tr" => (CountAny(joinedWords, [" ve ", " bir ", " bu ", " için ", " merhaba ", " teşekkür "]) * 0.60) * multiplier,
            "vi" => (CountAny(joinedWords, [" xin ", " chào ", " cảm ", " không ", " với "]) * 0.60) * multiplier,
            _ => 0.0,
        };
    }

    private static bool ContainsArabic(string text)
    {
        foreach (var character in text)
        {
            if ((character is >= '\u0600' and <= '\u06FF')
                || (character is >= '\u0750' and <= '\u077F')
                || (character is >= '\u08A0' and <= '\u08FF'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsHebrew(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u0590' and <= '\u05FF')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsThai(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u0E00' and <= '\u0E7F')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsTamil(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u0B80' and <= '\u0BFF')
            {
                return true;
            }
        }

        return false;
    }
}
