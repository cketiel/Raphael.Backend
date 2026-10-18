using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Raphael.Shared.Entities.Places;

namespace Raphael.Api.Services.Places
{
    /// <summary>
    /// Text rules of RE-054: how an address becomes a lookup key, how a comment is split into lines,
    /// and when two lines say the same thing. Pure functions, so they are tested without a database.
    /// </summary>
    public static class NoteText
    {
        private static readonly Dictionary<string, string> AddressWords = new(StringComparer.Ordinal)
        {
            ["street"] = "st", ["avenue"] = "ave", ["av"] = "ave", ["road"] = "rd", ["boulevard"] = "blvd",
            ["drive"] = "dr", ["lane"] = "ln", ["court"] = "ct", ["place"] = "pl", ["highway"] = "hwy",
            ["parkway"] = "pkwy", ["terrace"] = "ter", ["circle"] = "cir", ["square"] = "sq",
            ["north"] = "n", ["south"] = "s", ["east"] = "e", ["west"] = "w",
            ["northeast"] = "ne", ["northwest"] = "nw", ["southeast"] = "se", ["southwest"] = "sw",
            ["suite"] = "#", ["ste"] = "#", ["apartment"] = "#", ["apt"] = "#", ["unit"] = "#",
            ["usa"] = "", ["us"] = "",
        };

        /// <summary>
        /// The address as a lookup key: lower case, no punctuation, street words abbreviated
        /// ("Street" and "St." match). The unit stays: two clinics in one building are two places.
        /// </summary>
        public static string AddressKey(string? address)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;

            var cleaned = new StringBuilder(address.Length);
            foreach (var c in RemoveAccents(address.ToLowerInvariant()))
                cleaned.Append(char.IsLetterOrDigit(c) || c == '#' ? c : ' ');

            var words = cleaned.ToString()
                .Replace("#", " # ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => AddressWords.TryGetValue(w, out var abbreviation) ? abbreviation : w)
                .Where(w => w.Length > 0);

            // "united states" at the end is noise, like "usa".
            var key = string.Join(' ', words);
            if (key.EndsWith(" united states", StringComparison.Ordinal))
                key = key[..^" united states".Length];

            // "# 4" and "#4" are the same unit.
            key = key.Replace("# ", "#");
            return key.Length > 450 ? key[..450] : key;
        }

        /// <summary>
        /// Whether a trip's full address ("12 Main St, Miami, FL 33101") is the street address of a
        /// patient's record ("12 Main Street"). Customers keep street and city in separate fields.
        /// </summary>
        public static bool SameStreetAddress(string tripKey, string streetKey) =>
            streetKey.Length > 0
            && (tripKey == streetKey || tripKey.StartsWith(streetKey + " ", StringComparison.Ordinal));

        private static readonly Regex SentenceEnd = new(@"(?<=[.!?])\s+(?=[A-Z¿¡])", RegexOptions.Compiled);

        private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            "dr.", "mr.", "mrs.", "ms.", "st.", "ave.", "apt.", "ste.", "no.", "sr.", "jr.", "rd.", "blvd.", "bldg.", "fl.",
        };

        /// <summary>
        /// A comment as separate lines: by line break, by <c>;</c> and <c>|</c>, and by sentence, not
        /// breaking after "Dr." or "Apt.". Blank lines are dropped.
        /// </summary>
        public static List<string> SplitLines(string? text)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return lines;

            foreach (var chunk in text.Split(new[] { '\r', '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pending = string.Empty;
                foreach (var sentence in SentenceEnd.Split(chunk))
                {
                    var piece = pending.Length == 0 ? sentence : pending + " " + sentence;
                    var lastWord = piece.TrimEnd().Split(' ').LastOrDefault() ?? string.Empty;
                    if (Abbreviations.Contains(lastWord))
                    {
                        pending = piece;
                        continue;
                    }
                    Add(lines, piece);
                    pending = string.Empty;
                }
                Add(lines, pending);
            }
            return lines;

            static void Add(List<string> lines, string piece)
            {
                var trimmed = piece.Trim().Trim('-', '–', '•', ',').Trim();
                if (trimmed.Length > 0) lines.Add(trimmed);
            }
        }

        /// <summary>
        /// The form two lines are compared in: lower case, no accents, no punctuation, single spaces.
        /// "Dogs in the yard." and "dogs in the yard" are the same line.
        /// </summary>
        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var sb = new StringBuilder(text.Length);
            foreach (var c in RemoveAccents(text.ToLowerInvariant()))
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Whether <paramref name="line"/> adds nothing to <paramref name="known"/>: the same line, or
        /// one already contained in a known line ("gate code 1234" inside "gate code 1234, ring twice").
        /// </summary>
        public static bool IsKnown(string line, IEnumerable<string> known)
        {
            var n = Normalize(line);
            if (n.Length == 0) return true;
            foreach (var k in known)
            {
                var nk = Normalize(k);
                if (nk.Length == 0) continue;
                if (nk == n || (" " + nk + " ").Contains(" " + n + " ", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>The lines of <paramref name="text"/> that <paramref name="known"/> does not have yet.</summary>
        public static List<string> NewLines(string? text, IEnumerable<string> known)
        {
            var knownList = known.ToList();
            var result = new List<string>();
            foreach (var line in SplitLines(text))
            {
                if (line.Length > NoteRules.MaxNoteLength) continue; // a pasted essay is not a note
                if (IsKnown(line, knownList)) continue;
                result.Add(line);
                knownList.Add(line);
            }
            return result;
        }

        private static string RemoveAccents(string text)
        {
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
