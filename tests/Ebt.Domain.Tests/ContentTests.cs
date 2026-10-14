using System.Text.Json;

namespace Ebt.Domain.Tests;

public class ContentTests
{
    [Fact]
    public void Reads_the_data_format_written_by_the_pipeline()
    {
        const string json = """
            [{"n":301,"q":["Welches ",{"t":"Wappen","term":4}," gehört zu ",{"t":"NRW","u":true},"?"],
              "a":[["1"],["2"],["3"],["4"]],"c":2,"img":"data/img/17349.png","state":true}]
            """;

        var q = Assert.Single(JsonSerializer.Deserialize(json, DataJson.Default.QuestionArray)!);

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
