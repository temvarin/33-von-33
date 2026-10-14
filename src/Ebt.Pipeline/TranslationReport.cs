using System.Text.RegularExpressions;

namespace Ebt.Pipeline;

/// <summary>
/// Where a glossary term occurs in the German text, does the Russian translation use the glossary's Russian term?
/// Russian case endings make this approximate (it compares 4-letter word stems), so it is a list for manual review,
/// not a validation rule.
/// </summary>
internal static partial class TranslationReport
{
    public static IEnumerable<string> Find(SourceData src)
    {
        var matcher = new TermMatcher(src.Glossary);
        foreach (var answer in src.Answers.OrderBy(a => a.Nr))
        {
            var t = src.Translations[answer.Nr];
            var pairs = answer.Answers.Select((de, i) => (Where: $"a{i + 1}", De: de, Ru: t.A[i]))
                .Prepend(("q", src.Texts[answer.Nr], t.Q));

            foreach (var (where, de, ru) in pairs)
            {
                var plain = de.Replace("_", "");
                foreach (var m in matcher.Matches(plain))
                {
                    var term = src.Glossary[m.Term];
                    if (!Variants(term.Ru).Any(v => ContainsStems(ru, v)))
                        yield return $"{answer.Nr} {where}: {plain.Substring(m.Index, m.Length)} → «{term.Ru}» | {ru}";
                }
            }
        }
    }

    /// <summary>The Russian equivalents of a term: its comma-separated variants without bracketed glosses.</summary>
    private static string[] Variants(string ru) =>
        Gloss().Replace(ru, "").Split([',', ';', '/'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static bool ContainsStems(string text, string variant)
    {
        var have = Stems(text).ToHashSet();
        var need = Stems(variant).ToList();
        return need.Count > 0 && need.All(have.Contains);
    }

    private static IEnumerable<string> Stems(string s) =>
        Word().Matches(s.ToLowerInvariant().Replace('ё', 'е')).Select(m => m.Value[..Math.Min(4, m.Value.Length)]);

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex Gloss();

    [GeneratedRegex("[а-я]{3,}")]
    private static partial Regex Word();
}
