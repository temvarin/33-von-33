using Ebt.Domain;

namespace Ebt.Pipeline.Tests;

public class RichTextTests
{
    private static readonly TermMatcher Terms = new(
    [
        new("das Bundesland", ["Bundesland"], "федеральная земля", "…"),
        new("die Wahl", ["Wahl"], "выборы", "…"),
    ]);

    [Fact]
    public void Plain_text_is_one_segment() =>
        Assert.Equal([new Segment("Wie heißt die Verfassung?")], RichText.Parse("Wie heißt die Verfassung?", Terms));

    [Fact]
    public void Splits_emphasis_and_terms_into_segments() =>
        Assert.Equal(
            [new Segment("Was ist "), new Segment("kein", Underline: true), new Segment(" "), new Segment("Bundesland", Term: 0), new Segment("?")],
            RichText.Parse("Was ist _kein_ Bundesland?", Terms));

    [Fact]
    public void A_term_inside_emphasis_keeps_both() =>
        Assert.Equal([new Segment("Wahl", Underline: true, Term: 1)], RichText.Parse("_Wahl_", Terms));

    [Fact]
    public void Without_a_matcher_only_emphasis_is_parsed() =>
        Assert.Equal([new Segment("Что "), new Segment("не", Underline: true), new Segment(" относится?")], RichText.Parse("Что _не_ относится?"));

    [Fact]
    public void Serializes_plain_segments_as_strings_to_keep_the_data_small() =>
        Assert.Equal("""["Was ist ",{"t":"kein","u":true}," ",{"t":"Bundesland","term":0},"?"]""",
            Json.Write(RichText.Parse("Was ist _kein_ Bundesland?", Terms)));
}
