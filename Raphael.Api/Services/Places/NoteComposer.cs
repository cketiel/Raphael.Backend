using Raphael.Shared.Entities.Places;

namespace Raphael.Api.Services.Places
{
    /// <summary>The notes that apply to one end of one trip, already filtered to the ones that reach the driver.</summary>
    public sealed record StopNoteSources(
        string? TripNote,
        IReadOnlyList<string> EntranceNotes,
        IReadOnlyList<string> PlaceNotes,
        IReadOnlyList<string> CustomerNotes,
        string? FileComment);

    /// <summary>
    /// Builds the one-line comment the driver reads at a stop (RE-054), and reads a comment edited by
    /// hand back into what belongs to the trip alone. Pure, so it is tested without a database.
    /// </summary>
    public static class NoteComposer
    {
        /// <summary>
        /// This trip's own note first, then the entrance, the place, the patient, and whatever the
        /// file said that no note covers yet. A line already said is not said twice. Past
        /// <see cref="NoteRules.MaxComposedLength"/> the last lines are dropped.
        /// </summary>
        public static string? Compose(StopNoteSources sources)
        {
            var lines = new List<string>();
            AddAll(lines, NoteText.SplitLines(sources.TripNote));
            AddAll(lines, sources.EntranceNotes);
            AddAll(lines, sources.PlaceNotes);
            AddAll(lines, sources.CustomerNotes);
            AddAll(lines, NoteText.SplitLines(sources.FileComment));

            if (lines.Count == 0) return null;

            var result = string.Empty;
            foreach (var line in lines)
            {
                var next = result.Length == 0 ? line : result + NoteRules.Separator + line;
                if (next.Length > NoteRules.MaxComposedLength) break;
                result = next;
            }
            return result.Length == 0 ? lines[0][..Math.Min(lines[0].Length, NoteRules.MaxComposedLength)] : result;

            static void AddAll(List<string> lines, IEnumerable<string> candidates)
            {
                foreach (var candidate in candidates)
                {
                    var line = candidate?.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (NoteText.IsKnown(line, lines)) continue;
                    lines.Add(line);
                }
            }
        }

        /// <summary>
        /// What a hand-edited comment says beyond the notes and the file: that is this trip's own note.
        /// </summary>
        /// <remarks>
        /// The office used to type straight into the comment field, and Desktop up to 1.13 still sends
        /// the whole field on every save. Lines that are already a note or came from the file are not
        /// copied into the trip's note; a line deleted from the field does not delete the note (that is
        /// done from the notes window). Null when nothing is left.
        /// </remarks>
        public static string? TripNoteFromEditedComment(string? editedComment, StopNoteSources current)
        {
            var known = new List<string>();
            known.AddRange(current.EntranceNotes);
            known.AddRange(current.PlaceNotes);
            known.AddRange(current.CustomerNotes);
            known.AddRange(NoteText.SplitLines(current.FileComment));

            var own = new List<string>();
            foreach (var line in NoteText.SplitLines(editedComment))
            {
                if (NoteText.IsKnown(line, known) || NoteText.IsKnown(line, own)) continue;
                own.Add(line);
            }
            if (own.Count == 0) return null;

            var text = string.Join(NoteRules.Separator, own);
            return text.Length > NoteRules.MaxTripNoteLength ? text[..NoteRules.MaxTripNoteLength] : text;
        }
    }
}
