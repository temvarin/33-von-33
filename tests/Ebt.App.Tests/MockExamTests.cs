using Ebt.App.Features.MockExam;
using Ebt.App.Shared;
using Ebt.Learning;

namespace Ebt.App.Tests;

public class MockExamTests : AppTestContext
{
    private IRenderedComponent<WithTopBar> StartExam()
    {
        var page = Render<WithTopBar>(p => p.AddChildContent<MockExamPage>());
        page.FindAll("button").Single(b => b.TextContent == "Prüfung starten").Click();
        return page;
    }

    [Fact]
    public void The_timer_counts_down_from_60_minutes()
    {
        var page = StartExam();
        Assert.Equal("60:00", page.Find(".timer").TextContent);

        Clock.Advance(TimeSpan.FromMinutes(55) + TimeSpan.FromSeconds(1));

        page.WaitForAssertion(() => Assert.Equal("4:59", page.Find(".timer").TextContent));
        Assert.Contains("low", page.Find(".timer").ClassName);
    }

    [Fact]
    public void After_an_answer_the_exam_moves_on_to_the_next_question()
    {
        var page = StartExam();
        page.FindAll(".answer")[0].Click();
        Assert.Contains("Frage 1 von 33", page.Markup); // a short moment to see the choice

        Clock.Advance(TimeSpan.FromMilliseconds(250));

        page.WaitForAssertion(() => Assert.Contains("Frage 2 von 33", page.Markup));
    }

    [Fact]
    public async Task Going_to_a_question_right_after_an_answer_cancels_the_move_on()
    {
        var page = StartExam();
        page.FindAll(".answer")[0].Click();
        page.FindAll(".grid button")[9].Click();

        Clock.Advance(TimeSpan.FromSeconds(1));
        await page.InvokeAsync(() => { }); // runs after anything the delay had queued

        Assert.Contains("Frage 10 von 33", page.Markup);
        Assert.Equal(9, Progress.Exam!.Position);
    }

    [Fact]
    public void Answering_again_replaces_the_pending_move_on()
    {
        var page = StartExam();
        page.FindAll(".answer")[0].Click();
        page.FindAll(".grid button")[9].Click();
        page.WaitForAssertion(() => Assert.Contains("Frage 10 von 33", page.Markup));
        page.FindAll(".answer")[1].Click();

        Clock.Advance(TimeSpan.FromMilliseconds(250));

        // Next unanswered after question 10 is 11, not 2 as the first answer had planned.
        page.WaitForAssertion(() => Assert.Contains("Frage 11 von 33", page.Markup));
    }

    [Fact]
    public void When_the_time_is_up_the_exam_is_handed_in_and_the_user_told_why()
    {
        var page = StartExam();
        page.FindAll(".answer")[0].Click();

        Clock.Advance(Exam.Duration);

        page.WaitForAssertion(() => Assert.EndsWith("exam/result/0", Nav.Uri));
        Assert.Null(Progress.Exam);
        Assert.Single(Progress.Exams);
        Assert.Equal("Die Zeit ist abgelaufen. Die Prüfung wurde automatisch abgegeben.", Assert.IsType<MessageModal>(Dialogs.Current).Message);
    }

    [Fact]
    public void An_exam_left_without_any_answer_until_the_time_ran_out_is_discarded()
    {
        Progress.StartExam(Catalog.Questions, Clock.GetUtcNow(), new Random(1));
        Clock.Advance(Exam.Duration);

        var page = Render<MockExamPage>();

        Assert.Null(Progress.Exam);
        Assert.Empty(Progress.Exams);
        Assert.Contains("So läuft die Prüfung ab", page.Markup);
    }

    [Fact]
    public void The_exam_shows_no_glossary_terms_or_translation()
    {
        var page = StartExam();

        Assert.Empty(page.FindAll(".term, .ru-toggle"));
    }
}
