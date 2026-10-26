using Ebt.App.Features.MockExam;
using Ebt.App.Features.Practice;
using Ebt.App.Features.Review;
using Ebt.App.Shared;
using Ebt.Content;
using Ebt.Learning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Ebt.App.Tests;

/// <summary>The session services without any UI: every change is saved at once, and the exam keeps its time.</summary>
public sealed class SessionTests : IDisposable
{
    private static readonly Catalog Catalog = AppTestContext.Catalog;

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));
    private readonly AppTestContext.MemoryStore _storage = new();
    private readonly ServiceProvider _services;
    private readonly List<ExamEnd> _ended = [];

    public SessionTests()
    {
        _services = new ServiceCollection()
            .AddAppServices(Catalog)
            .AddSingleton<IKeyValueStore>(_storage)
            .AddSingleton<TimeProvider>(_clock)
            .AddSingleton(new Random(42))
            .BuildServiceProvider();
        Exams.Ended += _ended.Add;
    }

    public void Dispose() => _services.Dispose();

    private ExamSession Exams => _services.GetRequiredService<ExamSession>();
    private Progress Progress => _services.GetRequiredService<AppState>().Progress;

    /// <summary>What a fresh start of the app would load.</summary>
    private Progress Stored => new ProgressStore(_storage).Load();

    private static Task RunNow(Action work)
    {
        work();
        return Task.CompletedTask;
    }

    [Fact]
    public void A_practice_answer_is_saved_at_once()
    {
        var practice = _services.GetRequiredService<PracticeSession>();
        practice.Start(PracticeMode.Order);

        practice.Answer(Catalog[1], answer: 0); // wrong: 3 is correct
        practice.Next();

        Assert.Equal(1, Stored.Practice.Position);
        Assert.Equal(0, Stored.BoxOf(1));
        Assert.False(Stored.IsNew(1));
    }

    [Fact]
    public void A_review_answer_is_saved_at_once()
    {
        var review = _services.GetRequiredService<ReviewSession>();
        _services.GetRequiredService<PracticeSession>().Answer(Catalog[5], answer: 0);

        review.Answer(Catalog[review.Queue()[0]], Catalog[5].Correct);

        Assert.Equal(1, Stored.BoxOf(5));
    }

    [Fact]
    public async Task An_exam_answer_is_saved_at_once_and_the_exam_moves_on_after_a_moment()
    {
        Exams.Open(RunNow);
        Exams.Start();

        var answering = Exams.Answer(2);
        Assert.Equal([2], Stored.Exam!.Answers.Take(1));
        Assert.Equal(0, Stored.Exam.Position);

        _clock.Advance(ExamSession.AdvanceDelay);
        await answering;

        Assert.Equal(1, Stored.Exam!.Position);
    }

    [Fact]
    public void When_the_time_is_up_while_open_the_exam_is_handed_in()
    {
        Exams.Open(RunNow);
        Exams.Start();
        _ = Exams.Answer(0);

        _clock.Advance(Exam.Duration);

        Assert.Equal([ExamEnd.TimeUp], _ended);
        Assert.Null(Stored.Exam);
        Assert.Single(Stored.Exams);
    }

    [Fact]
    public async Task An_answer_after_the_time_is_up_is_not_taken_the_exam_is_handed_in_instead()
    {
        Exams.Start(); // the page is closed, so no clock is running
        Exams.GoTo(5);
        _clock.Advance(Exam.Duration);

        await Exams.Answer(1);

        Assert.Equal([ExamEnd.TimeUp], _ended);
        var result = Assert.Single(Progress.Exams);
        Assert.All(result.Answers, a => Assert.Null(a));
    }

    [Fact]
    public void An_exam_whose_time_ran_out_while_away_is_handed_in_when_the_page_opens()
    {
        Exams.Open(RunNow);
        Exams.Start();
        _ = Exams.Answer(0);
        Exams.Close();
        _clock.Advance(Exam.Duration);
        Assert.Empty(_ended); // closed: no clock

        Exams.Open(RunNow);

        Assert.Equal([ExamEnd.TimeRanOutWhileAway], _ended);
        Assert.Single(Stored.Exams);
    }

    [Fact]
    public void Handing_in_twice_keeps_one_result()
    {
        Exams.Start();

        Exams.HandIn();
        Exams.HandIn(); // e.g. confirmed after the clock had handed it in

        Assert.Equal([ExamEnd.HandedIn], _ended);
        Assert.Single(Stored.Exams);
    }
}
