using Ebt.App.Shared;
using Ebt.Content;
using Ebt.Learning;

namespace Ebt.App.Features.MockExam;

/// <summary>How an exam ended.</summary>
public enum ExamEnd
{
    /// <summary>The user handed it in.</summary>
    HandedIn,

    /// <summary>The time ran out while the exam was open; the user should be told why it ended.</summary>
    TimeUp,

    /// <summary>The time ran out while the exam page was closed; it was handed in when the page opened again.</summary>
    TimeRanOutWhileAway,
}

/// <summary>
/// The mock exam: starting, answering, moving between questions and handing in, every change saved at once.
/// While the exam page is open (<see cref="Open"/> to <see cref="Close"/>) it also runs the clock: it ticks every
/// second and hands the exam in when the time is up. After an answer it moves on to the next unanswered question
/// after a short moment, unless the user does something else first.
/// </summary>
public sealed class ExamSession(AppState state, Catalog catalog, TimeProvider clock, Random random) : IDisposable
{
    /// <summary>How long a chosen answer stays visible before the next question is shown.</summary>
    public static readonly TimeSpan AdvanceDelay = TimeSpan.FromMilliseconds(250);

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private Func<Action, Task>? _dispatch; // set while the exam page is open
    private ITimer? _timer;
    private CancellationTokenSource? _advance; // the pending move to the next question after an answer

    /// <summary>The exam in progress, if any.</summary>
    public Exam? Exam => state.Progress.Exam;

    /// <summary>Time left in the exam in progress; zero if there is none.</summary>
    public TimeSpan Remaining => Exam?.Remaining(clock.GetUtcNow()) ?? TimeSpan.Zero;

    /// <summary>A clock tick or a move to the next question: time to show the new state.</summary>
    public event Action? Changed;

    /// <summary>The exam ended; its result is the latest in <see cref="Progress.Exams"/>.</summary>
    public event Action<ExamEnd>? Ended;

    /// <summary>
    /// The exam page opened: drops an exam abandoned without answers, hands in one whose time ran out meanwhile,
    /// or starts the clock.
    /// </summary>
    /// <param name="dispatch">Runs the clock's work where the page renders (its <c>InvokeAsync</c>), never alongside a render.</param>
    public void Open(Func<Action, Task> dispatch)
    {
        _dispatch = dispatch;
        var now = clock.GetUtcNow();
        if (Exam?.IsAbandoned(now) == true) state.Change(p => p.DropAbandonedExam(now));
        if (Exam is not { } exam) return;
        if (exam.IsOver(now)) End(ExamEnd.TimeRanOutWhileAway);
        else StartClock();
    }

    /// <summary>The exam page closed: the clock stops and a pending move to the next question is dropped.</summary>
    public void Close()
    {
        _dispatch = null;
        CancelAdvance();
        StopClock();
    }

    public void Dispose() => Close();

    /// <summary>Starts a new exam, replacing one in progress.</summary>
    public void Start()
    {
        CancelAdvance();
        state.Change(p => p.StartExam(catalog.Questions, clock.GetUtcNow(), random));
        if (_dispatch is not null) StartClock();
    }

    /// <summary>
    /// Answers the current question, then moves on to the next unanswered one after <see cref="AdvanceDelay"/>.
    /// Anything the user does meanwhile – going to a question, answering again, handing in, leaving – cancels the move.
    /// Once the time is up, the exam is handed in instead.
    /// </summary>
    public async Task Answer(int answer)
    {
        if (Exam is not { } exam) return;
        CancelAdvance();
        var now = clock.GetUtcNow();
        if (exam.IsOver(now))
        {
            End(ExamEnd.TimeUp);
            return;
        }
        var next = state.Change(_ => exam.Answer(answer, now));

        var advance = _advance = new CancellationTokenSource();
        try
        {
            await Task.Delay(AdvanceDelay, clock, advance.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        state.Change(_ => exam.GoTo(next));
        Changed?.Invoke();
    }

    public void GoTo(int position)
    {
        if (Exam is not { } exam) return;
        CancelAdvance();
        state.Change(_ => exam.GoTo(position));
    }

    /// <summary>Hands the exam in. Does nothing if none is in progress any more: the clock may have handed it in meanwhile.</summary>
    public void HandIn()
    {
        if (Exam is not null) End(ExamEnd.HandedIn);
    }

    private void End(ExamEnd how)
    {
        CancelAdvance();
        StopClock();
        state.Change(p => p.FinishExam(catalog, clock.GetUtcNow()));
        Ended?.Invoke(how);
    }

    private void Tick()
    {
        if (Exam is not { } exam) return;
        if (exam.IsOver(clock.GetUtcNow())) End(ExamEnd.TimeUp);
        else Changed?.Invoke();
    }

    private void StartClock()
    {
        StopClock();
        _timer = clock.CreateTimer(_ => _dispatch?.Invoke(Tick), null, TickInterval, TickInterval);
    }

    private void StopClock()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void CancelAdvance()
    {
        _advance?.Cancel();
        _advance?.Dispose();
        _advance = null;
    }
}
