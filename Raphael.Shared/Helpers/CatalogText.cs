using System.Globalization;
using System.Text;

namespace Raphael.Shared.Helpers
{
    /// <summary>
    /// Turns what the source files and the users write into something that can be matched.
    /// </summary>
    /// <remarks>
    /// Everything here exists because the same fact arrives written several ways. The phone of
    /// one nursing home appears as "(407) 786-5637" in one file and "4078138000" in another; an
    /// administrator is "González" in the file and "gonzalez" in the search box. Rather than
    /// asking the database to be clever about it — a collation on the column, a full-text
    /// catalog — the row stores a cleaned copy and the search term is cleaned the same way.
    /// The comparison then happens between two strings that were prepared by the same code,
    /// which is the only way it stays true when either side changes.
    /// </remarks>
    public static class CatalogText
    {
        /// <summary>Longest a phone can be once the punctuation is gone. Matches the column.</summary>
        private const int MaxPhoneDigits = 20;

        private const int MaxSearchText = 1000;
        private const int MaxMatchKey = 200;

        /// <summary>
        /// Upper-cased, accents removed, runs of whitespace collapsed to one space, trimmed.
        /// </summary>
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var decomposed = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            var lastWasSpace = true; // starts true so a leading space is dropped

            foreach (var character in decomposed)
            {
                // FormD splits "á" into "a" plus a combining accent; dropping the marks is what
                // makes "gonzalez" and "González" the same string.
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    if (!lastWasSpace)
                    {
                        builder.Append(' ');
                        lastWasSpace = true;
                    }

                    continue;
                }

                builder.Append(char.ToUpperInvariant(character));
                lastWasSpace = false;
            }

            return builder.ToString().TrimEnd().Normalize(NormalizationForm.FormC);
        }

        /// <summary>Just the digits, capped at what the column holds.</summary>
        public static string? DigitsOnly(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var builder = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                if (char.IsDigit(character))
                {
                    builder.Append(character);
                }
            }

            if (builder.Length == 0)
            {
                return null;
            }

            return builder.Length > MaxPhoneDigits
                ? builder.ToString(0, MaxPhoneDigits)
                : builder.ToString();
        }

        /// <summary>
        /// The haystack a row is searched by: everything a person might type to find it,
        /// normalized and joined.
        /// </summary>
        public static string BuildSearchText(params string?[] parts)
        {
            var builder = new StringBuilder(MaxSearchText);

            foreach (var part in parts)
            {
                var normalized = Normalize(part);

                if (normalized.Length == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(normalized);

                if (builder.Length >= MaxSearchText)
                {
                    break;
                }
            }

            return builder.Length > MaxSearchText
                ? builder.ToString(0, MaxSearchText)
                : builder.ToString();
        }

        /// <summary>
        /// The natural key of a catalog row: its group, its name and its ZIP, with everything
        /// that is not a letter or a digit removed.
        /// </summary>
        /// <remarks>
        /// This is what makes importing the same file twice update the rows instead of doubling
        /// the catalog. It is deliberately not the name alone: two different nursing homes of the
        /// same chain share a name and differ by ZIP, and merging them would lose one of them.
        /// </remarks>
        public static string BuildMatchKey(int categoryId, string? name, string? zip)
        {
            var builder = new StringBuilder(MaxMatchKey);
            builder.Append(categoryId).Append('|');

            AppendAlphanumeric(builder, Normalize(name));
            builder.Append('|');
            AppendAlphanumeric(builder, Normalize(zip));

            return builder.Length > MaxMatchKey
                ? builder.ToString(0, MaxMatchKey)
                : builder.ToString();
        }

        /// <summary>
        /// Splits what the user typed into terms. Every term has to match something, which is
        /// what lets "harborview altamonte" find a row whose name and city each hold one word.
        /// </summary>
        public static IReadOnlyList<string> SplitTerms(string? term, int maxTerms = 6)
        {
            var normalized = Normalize(term);

            if (normalized.Length == 0)
            {
                return Array.Empty<string>();
            }

            var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length <= maxTerms
                ? parts
                : parts.Take(maxTerms).ToArray();
        }

        /// <summary>Whether a term is a phone number rather than a word.</summary>
        public static bool LooksLikePhone(string term)
        {
            if (term.Length < 3)
            {
                return false;
            }

            foreach (var character in term)
            {
                if (!char.IsDigit(character))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AppendAlphanumeric(StringBuilder builder, string value)
        {
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
            }
        }
    }
}
