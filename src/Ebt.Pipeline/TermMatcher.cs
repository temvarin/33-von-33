using System.Text.RegularExpressions;

namespace Ebt.Pipeline;

/// <summary>A glossary term found in a text: position and index into the glossary.</summary>
public readonly record struct TermMatch(int Index, int Length, int Term);

/// <summary>Finds glossary terms in German text. Assumes the glossary is valid (no duplicate forms, see <see cref="Validation"/>).</summary>
public sealed class TermMatcher
{
    private readonly Dictionary<string, int> _termByForm = [];
    private readonly Regex _regex;

    public TermMatcher(IReadOnlyList<GlossaryTerm> glossary)
    {
        for (var i = 0; i < glossary.Count; i++)
            foreach (var form in glossary[i].Forms)
                _termByForm[form] = i;

        // Longest forms first, so "Bundestagswahl" wins over "Bundestag".
        // A form only matches as a whole word: not preceded or followed by a letter or digit.
        var alternation = string.Join('|', _termByForm.Keys.OrderByDescending(f => f.Length).Select(Regex.Escape));
        _regex = new Regex($@"(?<![\p{{L}}\d])(?:{alternation})(?![\p{{L}}\d])", RegexOptions.CultureInvariant);
    }

    public IEnumerable<TermMatch> Matches(string text) =>
        _regex.Matches(text).Select(m => new TermMatch(m.Index, m.Length, _termByForm[m.Value]));
}
