namespace Ebt.Learning.Tests;

public class ProgressTests
{
    [Fact]
    public void A_correct_answer_moves_a_question_up_one_box_up_to_the_top_box()
    {
        var p = new Progress();
        for (var i = 0; i < Progress.MasteredBox + 2; i++) p.Record(7, correct: true);

        Assert.Equal(Progress.MasteredBox, p.BoxOf(7));
        Assert.False(p.Wrong.ContainsKey(7));
    }

    [Fact]
    public void A_wrong_answer_sends_a_question_back_to_box_0_and_counts_the_mistake()
    {
        var p = new Progress();
        p.Record(7, true);
        p.Record(7, true);
        p.Record(7, false);

        Assert.Equal(0, p.BoxOf(7));
        Assert.Equal(1, p.Wrong[7]);
    }

    [Fact]
    public void The_mistakes_queue_holds_unlearned_mistakes_weakest_box_first()
    {
        var questions = Fixtures.Catalog(5, 0).Questions;
        var p = new Progress();
        p.Record(1, false); // box 0
        p.Record(2, false); p.Record(2, true); p.Record(2, true); // box 2
        p.Record(3, false); p.Record(3, true); // box 1
        p.Record(4, false); for (var i = 0; i < Progress.MasteredBox; i++) p.Record(4, true); // learned again
        p.Record(5, true); // never wrong

        Assert.Equal([1, 3, 2], p.MistakeQueue(questions, Fixtures.Random()));
    }

    [Fact]
    public void Practice_order_is_in_order_shuffled_or_only_questions_never_answered()
    {
        var questions = Fixtures.Catalog(6, 0).Questions;
        var p = new Progress();
        p.Record(2, true);
        p.Record(5, false);

        int[] Order(PracticeMode mode)
        {
            p.StartPractice(mode, questions, Fixtures.Random());
            return [.. p.Practice.Order];
        }

        Assert.Equal([1, 2, 3, 4, 5, 6], Order(PracticeMode.Order));
        Assert.Equal([1, 2, 3, 4, 5, 6], Order(PracticeMode.Random).Order());
        Assert.Equal([1, 3, 4, 6], Order(PracticeMode.Unseen).Order());
    }

    [Fact]
    public void A_practice_run_stays_within_its_questions_and_ends_after_the_last()
    {
        var p = new Progress();
        p.StartPractice(PracticeMode.Order, Fixtures.Catalog(2, 0).Questions, Fixtures.Random());
        var run = p.Practice;

        run.Previous();
        Assert.Equal(1, run.Current);
        run.Next();
        Assert.Equal(2, run.Current);
        run.Next();
        run.Next();
        Assert.True(run.IsFinished);
        run.Previous();
        Assert.Equal(2, run.Current);
    }

    [Fact]
    public void Box_counts_and_mastered_count_cover_every_question()
    {
        var questions = Fixtures.Catalog(4, 0).Questions;
        var p = new Progress();
        p.Record(1, false);
        for (var i = 0; i < Progress.MasteredBox; i++) p.Record(2, true);

        var counts = p.CountBoxes(questions);

        Assert.Equal(2, counts.New);
        Assert.Equal([1, 0, 0, 1], counts.InBox);
        Assert.Equal(1, p.CountMastered(questions));
    }

    [Fact]
    public void A_run_can_start_at_a_question_by_its_number_not_its_position()
    {
        var questions = Fixtures.Catalog(3, 2).Questions; // 1, 2, 3, 301, 302: no question 4
        var p = new Progress();

        Assert.True(p.StartPracticeAt(301, questions));
        Assert.Equal((PracticeMode.Order, 301), (p.Practice.Mode, p.Practice.Current));

        Assert.False(p.StartPracticeAt(4, questions));
        Assert.Equal(301, p.Practice.Current); // the run stays
    }

    [Fact]
    public void Answers_are_recorded_against_the_correct_one_and_mistakes_counted_until_learned()
    {
        var questions = Fixtures.Catalog(4, 0).Questions;
        var q2 = questions[1];
        var p = new Progress();

        p.RecordAnswer(q2, (q2.Correct + 1) % 4);
        Assert.Equal(0, p.BoxOf(2));
        Assert.Equal(1, p.CountMistakes(questions));
        Assert.Equal([2], p.MistakeQueue(questions, Fixtures.Random()));

        for (var i = 0; i < Progress.MasteredBox; i++) p.RecordAnswer(q2, q2.Correct);
        Assert.Equal(Progress.MasteredBox, p.BoxOf(2));
        Assert.Equal(0, p.CountMistakes(questions));
    }
}
