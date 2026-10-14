namespace Ebt.Domain.Tests;

public class ExamTests
{
    private static readonly Catalog Catalog = Fixtures.Catalog();
    private static readonly DateTimeOffset Now = Fixtures.Now;

    [Fact]
    public void An_exam_has_30_general_and_3_state_questions_each_at_most_once()
    {
        var exam = NewExam();

        Assert.Equal(Exam.GeneralQuestions + Exam.StateQuestions, exam.Questions.Count);
        Assert.Equal(Exam.StateQuestions, exam.Questions.Count(n => Catalog[n].IsState));
        Assert.Equal(exam.Questions.Count, exam.Questions.Distinct().Count());
        Assert.All(exam.Answers, a => Assert.Null(a));
    }

    [Fact]
    public void The_time_runs_out_after_60_minutes()
    {
        var exam = NewExam();

        Assert.Equal(TimeSpan.FromMinutes(60), exam.Remaining(Now));
        Assert.False(exam.IsOver(Now.AddMinutes(60).AddTicks(-1)));
        Assert.True(exam.IsOver(Now.AddMinutes(60)));
    }

    [Fact]
    public void An_expired_exam_without_any_answer_is_abandoned_not_a_result()
    {
        var exam = NewExam();
        var end = Now + Exam.Duration;

        Assert.False(exam.IsAbandoned(end.AddTicks(-1)));
        Assert.True(exam.IsAbandoned(end));
        exam.Answer(1);
        Assert.False(exam.IsAbandoned(end));
    }

    [Fact]
    public void Answering_leads_to_the_next_unanswered_question()
    {
        var exam = NewExam();
        exam.GoTo(2);
        exam.Answer(0);
        exam.GoTo(1);

        Assert.Equal(3, exam.Answer(0)); // 2 is answered already
    }

    [Fact]
    public void Answering_the_last_question_with_none_open_after_it_stays_there()
    {
        var exam = NewExam();
        exam.GoTo(exam.Questions.Count - 1);

        Assert.Equal(exam.Questions.Count - 1, exam.Answer(0));
    }

    private static Exam NewExam(Progress? progress = null) =>
        (progress ?? new Progress()).StartExam(Catalog.Questions, Now, Fixtures.Random());

    /// <summary>The first <paramref name="correct"/> answers right, the last one wrong, the rest unanswered.</summary>
    private static (Progress Progress, Exam Exam, ExamResult Result) FinishWith(int correct)
    {
        var progress = new Progress();
        var exam = NewExam(progress);
        for (var i = 0; i < correct; i++) AnswerAt(exam, i, Catalog[exam.Questions[i]].Correct);
        var last = exam.Questions.Count - 1;
        AnswerAt(exam, last, (Catalog[exam.Questions[last]].Correct + 1) % 4);
        return (progress, exam, progress.FinishExam(Catalog, Now));
    }

    private static void AnswerAt(Exam exam, int position, int answer)
    {
        exam.GoTo(position);
        exam.Answer(answer);
    }

    [Fact]
    public void Seventeen_correct_answers_pass_sixteen_fail()
    {
        Assert.Equal(17, Exam.PassMark);
        Assert.True(FinishWith(17).Result.Passed);
        Assert.False(FinishWith(16).Result.Passed);
    }

    [Fact]
    public void Finishing_records_answered_questions_not_unanswered_ones_and_keeps_the_result()
    {
        var (progress, exam, result) = FinishWith(10);

        Assert.Equal(10, result.Score);
        Assert.Null(progress.Exam);
        Assert.Equal([result], progress.Exams);
        Assert.Equal(1, progress.BoxOf(exam.Questions[0])); // answered correctly
        Assert.True(progress.IsNew(exam.Questions[20])); // unanswered: not a mistake
        Assert.Equal(1, progress.Wrong[exam.Questions[^1]]); // answered wrong
    }

    [Fact]
    public void Only_the_latest_results_are_kept_newest_first()
    {
        var progress = new Progress();
        for (var i = 0; i < ExamResult.Kept + 5; i++)
        {
            progress.StartExam(Catalog.Questions, Now.AddDays(i), Fixtures.Random());
            progress.FinishExam(Catalog, Now.AddDays(i));
        }

        Assert.Equal(ExamResult.Kept, progress.Exams.Count);
        Assert.Equal(Now.AddDays(ExamResult.Kept + 4), progress.Exams[0].Date);
    }
}
