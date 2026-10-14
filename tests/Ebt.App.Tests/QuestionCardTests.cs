using Ebt.App.Components;
using Ebt.App.Services;

namespace Ebt.App.Tests;

public class QuestionCardTests : AppTestContext
{
    private readonly List<int> _answers = [];

    private IRenderedComponent<QuestionCard> RenderCard(bool help = true, int? chosen = null, bool reveal = false) =>
        Render<QuestionCard>(p => p
            .Add(c => c.Question, Catalog[1])
            .Add(c => c.Label, "Frage 1")
            .Add(c => c.Help, help)
            .Add(c => c.Chosen, chosen)
            .Add(c => c.Reveal, reveal)
            .Add(c => c.Locked, reveal)
            .Add(c => c.OnAnswer, (int a) => _answers.Add(a)));

    [Fact]
    public void Tapping_an_answer_chooses_it()
    {
        var card = RenderCard();

        card.FindAll(".answer")[2].Click();

        Assert.Equal([2], _answers);
    }

    [Fact]
    public void Tapping_a_glossary_term_inside_an_answer_explains_the_term_instead_of_answering()
    {
        var card = RenderCard();

        card.Find(".answer .term").Click();

        Assert.Equal(new TermModal(Steuern), Dialogs.Current);
        Assert.Empty(_answers);
    }

    [Fact]
    public void The_translation_button_shows_the_Russian_lines()
    {
        var card = RenderCard();
        Assert.Empty(card.FindAll(".ru-line"));

        card.Find(".ru-toggle").Click();

        Assert.Equal("true", card.Find(".ru-toggle").GetAttribute("aria-pressed"));
        Assert.Equal("Вопрос 1", card.Find(".qtext + .ru-line").TextContent);
        Assert.Equal("налоги", Assert.Single(card.FindAll(".answer .ru-line")).TextContent); // only where it differs from the German
    }

    [Fact]
    public void Without_help_as_in_the_exam_there_are_no_terms_and_no_translation()
    {
        var card = RenderCard(help: false);

        Assert.Empty(card.FindAll(".term"));
        Assert.Empty(card.FindAll(".ru-toggle, .ru-line"));
        Assert.Equal("die Menschen Steuern zahlen", card.FindAll(".answer")[1].TextContent.Replace("B", "").Trim());
    }

    [Fact]
    public void After_a_wrong_answer_it_sinks_in_the_correct_one_lifts_out_and_the_rest_fades()
    {
        var card = RenderCard(chosen: 1, reveal: true);

        var answers = card.FindAll(".answer");
        Assert.Equal(["dim", "pressed", "dim", "lifted"], answers.Select(a => a.ClassList.Intersect(["dim", "pressed", "lifted"]).Single()));
        Assert.Equal("Deine Antwort", answers[1].QuerySelector(".tag.bad")?.TextContent);
        Assert.Equal("Richtig", answers[3].QuerySelector(".tag.ok")?.TextContent);

        answers[0].Click();
        Assert.Empty(_answers); // locked
    }

    [Fact]
    public void Faded_answers_lose_their_glossary_highlights()
    {
        var card = RenderCard(chosen: 3, reveal: true); // right: only D stays prominent

        Assert.Contains("dim", card.FindAll(".answer")[1].ClassName);
        Assert.Empty(card.FindAll(".answer .term"));
        Assert.Single(card.FindAll(".answer .tag"));
    }
}
