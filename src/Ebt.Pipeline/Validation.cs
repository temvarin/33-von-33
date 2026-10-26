using System.Text.RegularExpressions;
using Ebt.Content;

namespace Ebt.Pipeline;

/// <summary>The question numbers a catalogue consists of: general questions from 1, state questions from 301.</summary>
public sealed record CatalogShape(int General, int State)
{
    /// <summary>Nordrhein-Westfalen: 300 general and 10 state questions.</summary>
    public static readonly CatalogShape Nrw = new(300, 10);

    public IEnumerable<int> Numbers =>
        Enumerable.Range(1, General).Concat(Enumerable.Range(Question.FirstStateNumber, State));
}

/// <summary>Content rules for the sources. Every rule returns human-readable problems; none means the data may be built.</summary>
public static partial class Validation
{
    public static IReadOnlyList<string> Check(SourceData src, CatalogShape shape) =>
        [.. Completeness(src, shape), .. Questions(src), .. Translations(src), .. Glossary(src)];

    /// <summary>Every question of the catalogue exactly once, and nothing else.</summary>
    internal static IEnumerable<string> Completeness(SourceData src, CatalogShape shape)
    {
        foreach (var twice in src.Answers.GroupBy(a => a.Nr).Where(g => g.Count() > 1).OrderBy(g => g.Key))
            yield return $"{twice.Key}: question appears {twice.Count()} times";

        var expected = shape.Numbers.ToHashSet();
        var actual = src.Answers.Select(a => a.Nr).ToHashSet();
        var missing = expected.Except(actual).Order().ToList();
        if (missing.Count > 0) yield return $"questions missing: {Ranges(missing)}";
        foreach (var nr in actual.Except(expected).Order())
            yield return $"{nr}: not a question of this catalogue (expected {Ranges(shape.Numbers)})";
    }

    internal static IEnumerable<string> Questions(SourceData src)
    {
        var numbers = src.Answers.Select(a => a.Nr).ToHashSet();
        foreach (var a in src.Answers)
        {
            if (!src.Texts.TryGetValue(a.Nr, out var text) || text.Length == 0) yield return $"{a.Nr}: no question text";
            else if (Count(text, '_') % 2 != 0) yield return $"{a.Nr}: unbalanced _ in question text";
            if (a.Answers.Length != 4 || a.Answers.Any(string.IsNullOrWhiteSpace)) yield return $"{a.Nr}: needs 4 non-empty answers";
            if (a.Correct is < 0 or > 3) yield return $"{a.Nr}: correct answer index {a.Correct} out of range";
        }
        foreach (var nr in src.Texts.Keys.Where(nr => !numbers.Contains(nr)))
            yield return $"{nr}: question text without answers";
        foreach (var id in src.PictureIds.Where(id => src.Answers.All(a => a.Id != id)))
            yield return $"pictures/{id}.png belongs to no question";
    }

    internal static IEnumerable<string> Translations(SourceData src)
    {
        foreach (var a in src.Answers)
        {
            if (!src.Translations.TryGetValue(a.Nr, out var t)) { yield return $"{a.Nr}: translation missing"; continue; }
            if (string.IsNullOrWhiteSpace(t.Q)) yield return $"{a.Nr}: translated question is empty";
            if (t.A is not { Length: 4 } || t.A.Any(string.IsNullOrWhiteSpace)) { yield return $"{a.Nr}: needs 4 translated answers"; continue; }

            var de = src.Texts.GetValueOrDefault(a.Nr, "");
            if (Count(de, '_') > 0 && Count(t.Q, '_') == 0) yield return $"{a.Nr}: emphasis (_…_) lost in translation";
            if (Count(t.Q, '_') % 2 != 0) yield return $"{a.Nr}: unbalanced _ in translated question";
            if (!Cyrillic().IsMatch(t.Q)) yield return $"{a.Nr}: question not translated";

            for (var i = 0; i < 4; i++)
            {
                if (NumbersOnly().IsMatch(a.Answers[i]) && t.A[i] != a.Answers[i])
                    yield return $"{a.Nr}: answer {i + 1} \"{a.Answers[i]}\" must stay unchanged, got \"{t.A[i]}\"";
            }

            // A gloss in brackets only on the correct option gives the answer away.
            var bracketed = Enumerable.Range(0, 4).Where(i => t.A[i].Contains('(') && !a.Answers[i].Contains('(')).ToList();
            if (bracketed is [var only] && only == a.Correct)
                yield return $"{a.Nr}: only the correct answer has a bracketed gloss – it gives the answer away";
        }
        foreach (var nr in src.Translations.Keys.Where(nr => src.Answers.All(a => a.Nr != nr)))
            yield return $"{nr}: translation for an unknown question";
    }

    internal static IEnumerable<string> Glossary(SourceData src)
    {
        var owner = new Dictionary<string, string>();
        foreach (var term in src.Glossary)
        {
            if (string.IsNullOrWhiteSpace(term.Ru) || string.IsNullOrWhiteSpace(term.Note)) yield return $"glossary \"{term.De}\": translation or note missing";
            if (term.Forms.Length == 0) yield return $"glossary \"{term.De}\": no word forms";
            foreach (var form in term.Forms)
            {
                if (!owner.TryAdd(form, term.De)) yield return $"glossary: form \"{form}\" in both \"{owner[form]}\" and \"{term.De}\"";
            }
        }
        if (owner.Count != src.Glossary.Sum(t => t.Forms.Length)) yield break; // the matcher needs unique forms

        var matcher = new TermMatcher(src.Glossary);
        var used = src.Answers
            .SelectMany(a => a.Answers.Prepend(src.Texts.GetValueOrDefault(a.Nr, "")))
            .SelectMany(text => matcher.Matches(text.Replace("_", "")))
            .Select(m => m.Term)
            .ToHashSet();
        for (var i = 0; i < src.Glossary.Count; i++)
        {
            if (!used.Contains(i)) yield return $"glossary \"{src.Glossary[i].De}\": occurs in no question";
        }
    }

    private static int Count(string s, char c) => s.Count(x => x == c);

    /// <summary>Sorted numbers as ranges: 2, 5–9, 301–310.</summary>
    private static string Ranges(IEnumerable<int> sorted)
    {
        var ranges = new List<(int From, int To)>();
        foreach (var n in sorted)
        {
            if (ranges.Count > 0 && ranges[^1].To == n - 1) ranges[^1] = (ranges[^1].From, n);
            else ranges.Add((n, n));
        }
        return string.Join(", ", ranges.Select(r => r.From == r.To ? $"{r.From}" : $"{r.From}–{r.To}"));
    }

    [GeneratedRegex(@"[а-яё]", RegexOptions.IgnoreCase)]
    private static partial Regex Cyrillic();

    /// <summary>Numbers, years, percentages – answers that must not be translated.</summary>
    [GeneratedRegex(@"^[\d\s%.,–-]+$")]
    private static partial Regex NumbersOnly();
}
