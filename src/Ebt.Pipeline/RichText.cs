using Ebt.Domain;

namespace Ebt.Pipeline;

/// <summary>
/// Turns source text into segments, so the app renders text without parsing it:
/// "_word_" becomes an underlined segment, glossary word forms become term segments.
/// </summary>
public static class RichText
{
    public static IReadOnlyList<Segment> Parse(string text, TermMatcher? terms = null)
    {
        var segments = new List<Segment>();
        var parts = text.Split('_'); // odd parts are between underscores, i.e. emphasized
        for (var i = 0; i < parts.Length; i++)
        {
            var underline = i % 2 == 1;
            var part = parts[i];
            var pos = 0;
            foreach (var m in terms?.Matches(part) ?? [])
            {
                Add(segments, part[pos..m.Index], underline, null);
                Add(segments, part.Substring(m.Index, m.Length), underline, m.Term);
                pos = m.Index + m.Length;
            }
            Add(segments, part[pos..], underline, null);
        }
        return segments;
    }

    private static void Add(List<Segment> segments, string text, bool underline, int? term)
    {
        if (text.Length == 0) return;
        // Merge neighbouring plain pieces of the same style into one segment.
        if (term is null && segments is [.., { Term: null } last] && last.Underline == underline)
            segments[^1] = last with { Text = last.Text + text };
        else
            segments.Add(new Segment(text, underline, term));
    }
}
