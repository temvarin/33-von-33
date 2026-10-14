namespace Ebt.Pipeline.Tests;

public class TermMatcherTests
{
    private static readonly GlossaryTerm[] Glossary =
    [
        new("der Bundestag", ["Bundestag", "Bundestages"], "бундестаг", "…"),
        new("die Bundestagswahl", ["Bundestagswahl"], "выборы в бундестаг", "…"),
        new("der Bürger", ["Bürger"], "гражданин", "…"),
        new("die Agentur für Arbeit", ["Agentur für Arbeit"], "агентство занятости", "…"),
    ];

    private static List<(string Text, int Term)> Find(string text) =>
        [.. new TermMatcher(Glossary).Matches(text).Select(m => (text.Substring(m.Index, m.Length), m.Term))];

    [Fact]
    public void Longer_form_wins_over_a_form_it_starts_with() =>
        Assert.Equal([("Bundestagswahl", 1)], Find("Die Bundestagswahl ist frei."));

    [Fact]
    public void Matches_inflected_forms_listed_in_the_glossary() =>
        Assert.Equal([("Bundestages", 0)], Find("Abgeordnete des Bundestages"));

    [Theory]
    [InlineData("Bundestagsabgeordnete")] // compound noun containing a form
    [InlineData("Bürgerinnen")]          // umlaut word continues after the form
    [InlineData("Mitbürger")]            // form at the end of a longer word
    public void Does_not_match_inside_longer_words(string text) => Assert.Empty(Find(text));

    [Fact]
    public void Matches_multi_word_forms() =>
        Assert.Equal([("Agentur für Arbeit", 3)], Find("zur Agentur für Arbeit gehen"));

    [Fact]
    public void Finds_every_occurrence_with_its_position()
    {
        var matches = new TermMatcher(Glossary).Matches("Bürger wählen den Bundestag.").ToList();
        Assert.Equal([new TermMatch(0, 6, 2), new TermMatch(18, 9, 0)], matches);
    }
}
