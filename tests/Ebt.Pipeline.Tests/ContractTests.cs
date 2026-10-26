using System.Text.Json;
using Ebt.Content;

namespace Ebt.Pipeline.Tests;

/// <summary>
/// The data contract from end to end: what the pipeline writes, read back the way the app loads it
/// (<see cref="ContentJson"/>, source-generated as in the WebAssembly build).
/// </summary>
public class ContractTests
{
    [Fact]
    public void The_app_reads_what_the_pipeline_writes()
    {
        var src = Fixtures.Valid(
            answers:
            [
                new BamfAnswer(1, 100, 1, ["Monarchie", "Republik", "Diktatur", "1949"]),
                new BamfAnswer(301, 200, 2, ["1", "2", "3", "4"]),
            ],
            texts: new Dictionary<int, string> { [1] = "Ist Deutschland eine _Republik_?", [301] = "Welches Wappen?" },
            translations: new Dictionary<int, Translation>
            {
                [1] = new("Германия – _республика_?", ["монархия", "республика", "диктатура", "1949"]),
                [301] = new("Какой герб?", ["1", "2", "3", "4"]),
            },
            pictures: new HashSet<int> { 200 });

        var files = DataBuilder.Build(src).JsonFiles;
        var questions = JsonSerializer.Deserialize(files[ContentJson.QuestionsFile], ContentJson.Questions)!;
        var ru = JsonSerializer.Deserialize(files[ContentJson.TranslationsFile], ContentJson.Translations)!;
        var terms = JsonSerializer.Deserialize(files[ContentJson.TermsFile], ContentJson.Terms)!;

        var (general, state) = (questions[0], questions[1]);
        Assert.Equal((1, 1, false, null), (general.Number, general.Correct, general.IsState, general.Image));
        Assert.Equal([new("Ist Deutschland eine "), new("Republik", Underline: true, Term: 0), new("?")], general.Text);
        Assert.Equal([new("Republik", Term: 0)], general.Answers[1]);
        Assert.Equal((301, 2, true, "data/img/200.png"), (state.Number, state.Correct, state.IsState, state.Image));

        Assert.Equal([1, 301], ru.Keys);
        Assert.Equal([new("Германия – "), new("республика", Underline: true), new("?")], ru[1].Text);
        Assert.Equal("диктатура", Assert.Single(ru[1].Answers[2]).Text);

        Assert.Equal(new Term("die Republik", "республика", "Государство с выборным главой."), Assert.Single(terms));
    }
}
