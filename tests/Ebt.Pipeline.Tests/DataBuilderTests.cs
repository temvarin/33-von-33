using System.Text.Json;
using Ebt.Content;

namespace Ebt.Pipeline.Tests;

public class DataBuilderTests
{
    [Fact]
    public void Marks_state_questions_and_links_pictures()
    {
        var src = Fixtures.Valid(
            answers:
            [
                new BamfAnswer(300, 100, 0, ["a", "b", "c", "d"]),
                new BamfAnswer(301, 200, 1, ["1", "2", "3", "4"]),
            ],
            texts: new Dictionary<int, string> { [300] = "Frage?", [301] = "Welches Wappen?" },
            translations: new Dictionary<int, Translation>
            {
                [300] = new("Вопрос?", ["а", "б", "в", "г"]),
                [301] = new("Какой герб?", ["1", "2", "3", "4"]),
            },
            pictures: new HashSet<int> { 200 });

        var output = DataBuilder.Build(src);
        var questions = JsonSerializer.Deserialize(output.JsonFiles[ContentJson.QuestionsFile], ContentJson.Questions)!;

        Assert.Equal([false, true], questions.Select(q => q.IsState));
        Assert.Equal([null, "data/img/200.png"], questions.Select(q => q.Image));
        Assert.Equal([200], output.PictureIds);
    }

    [Fact]
    public void Writes_translations_keyed_and_ordered_by_question_number()
    {
        var src = Fixtures.Valid(
            answers: [new BamfAnswer(2, 101, 0, ["a", "b", "c", "d"]), new BamfAnswer(1, 100, 0, ["a", "b", "c", "d"])],
            texts: new Dictionary<int, string> { [1] = "Eins?", [2] = "Zwei?" },
            translations: new Dictionary<int, Translation> { [2] = new("Два?", ["а", "б", "в", "г"]), [1] = new("Один?", ["а", "б", "в", "г"]) });

        var json = DataBuilder.Build(src).JsonFiles["ru.json"];

        Assert.StartsWith("""{"1":{"q":["Один?"]""", json);
    }

    [Fact]
    public void Ships_the_glossary_without_word_forms()
    {
        var json = DataBuilder.Build(Fixtures.Valid()).JsonFiles["terms.json"];

        Assert.Equal("""[{"de":"die Republik","ru":"республика","note":"Государство с выборным главой."}]""", json);
    }
}
