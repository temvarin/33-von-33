using System.Text.Json;

namespace Ebt.Content.Tests;

public class ContentTests
{
    [Fact]
    public void Writes_compact_data_with_plain_segments_as_strings_and_readable_Cyrillic()
    {
        Question[] questions =
        [
            new(1, [new("Was ist "), new("kein", Underline: true), new(" "), new("Bundesland", Term: 0), new("?")],
                [[new("a")], [new("b")], [new("c")], [new("d")]], Correct: 0),
        ];
        var ru = new SortedDictionary<int, Translation>
        {
            [1] = new([new("Что "), new("не", Underline: true), new(" относится?")], [[new("а")], [new("б")], [new("в")], [new("г")]]),
        };

        Assert.Equal(
            """[{"n":1,"q":["Was ist ",{"t":"kein","u":true}," ",{"t":"Bundesland","term":0},"?"],"a":[["a"],["b"],["c"],["d"]],"c":0}]""",
            JsonSerializer.Serialize(questions, ContentJson.Questions));
        Assert.Equal(
            """{"1":{"q":["Что ",{"t":"не","u":true}," относится?"],"a":[["а"],["б"],["в"],["г"]]}}""",
            JsonSerializer.Serialize(ru, ContentJson.Translations));
    }

    [Fact]
    public void Reads_the_compact_data_format()
    {
        const string json = """
            [{"n":301,"q":["Welches ",{"t":"Wappen","term":4}," gehört zu ",{"t":"NRW","u":true},"?"],
              "a":[["1"],["2"],["3"],["4"]],"c":2,"img":"data/img/17349.png","state":true}]
            """;

        var q = Assert.Single(JsonSerializer.Deserialize(json, ContentJson.Questions)!);

        Assert.Equal(
            [new("Welches "), new("Wappen", Term: 4), new(" gehört zu "), new("NRW", Underline: true), new("?")],
            q.Text);
        Assert.Equal((2, "data/img/17349.png", true, 1), (q.Correct, q.Image, q.IsState, q.CatalogNumber));
    }

    [Theory]
    [InlineData("der Bundestag", "bundestag")]
    [InlineData("der / die Abgeordnete", "abgeordnete")]
    [InlineData("die Würde", "wurde")]
    [InlineData("die Prügelstrafe", "prugelstrafe")]
    [InlineData("das Gesetz", "gesetz")]
    [InlineData("die Straße", "strasse")]
    public void Glossary_sorts_by_the_noun_in_German_dictionary_order(string de, string key) =>
        Assert.Equal(key, new Term(de, "", "").SortKey);
}
