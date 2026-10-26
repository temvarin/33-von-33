using Ebt.Content;

namespace Ebt.Learning;

public enum PracticeMode { Order, Random, Unseen }

/// <summary>Where the user is in a practice run.</summary>
public sealed class PracticeRun
{
    internal PracticeRun(PracticeMode mode, IReadOnlyList<int> order, int position)
    {
        Mode = mode;
        Order = order;
        Position = position;
    }

    /// <summary>No run started yet.</summary>
    internal static PracticeRun None => new(PracticeMode.Order, [], 0);

    public PracticeMode Mode { get; }
    public IReadOnlyList<int> Order { get; }

    /// <summary>Index into <see cref="Order"/>; equal to its length when the run is finished.</summary>
    public int Position { get; private set; }

    public bool IsFinished => Position >= Order.Count;

    /// <summary>The number of the question at the current position.</summary>
    public int Current => Order[Position];

    public void Next() => Position = Math.Min(Position + 1, Order.Count);

    public void Previous() => Position = Math.Max(Position - 1, 0);
}

/// <summary>
/// The user's learning progress as a Leitner system: a correct answer moves a question up one box,
/// a wrong one back to box 0. A question in the top box counts as learned. Persisted by <see cref="ProgressStore"/>.
/// </summary>
/// <remarks>State changes only through its methods. <see cref="ProgressStore"/> stores it in its own format.</remarks>
public sealed class Progress
{
    public const int MasteredBox = 3;

    public Progress() : this([], [], PracticeRun.None, null, []) { }

    /// <summary>Progress as it was stored.</summary>
    internal Progress(Dictionary<int, int> box, Dictionary<int, int> wrong, PracticeRun practice, Exam? exam, IReadOnlyList<ExamResult> exams)
    {
        Box = box;
        Wrong = wrong;
        Practice = practice;
        Exam = exam;
        Exams = exams;
    }

    /// <summary>Leitner box (0–3) per question number; absent = never answered.</summary>
    internal Dictionary<int, int> Box { get; }

    /// <summary>How often each question was answered wrong.</summary>
    internal Dictionary<int, int> Wrong { get; }

    public PracticeRun Practice { get; private set; }

    /// <summary>The exam in progress, if any.</summary>
    public Exam? Exam { get; private set; }

    /// <summary>Finished exams, newest first.</summary>
    public IReadOnlyList<ExamResult> Exams { get; private set; }

    public void Record(int number, bool correct)
    {
        if (correct)
        {
            Box[number] = Math.Min(MasteredBox, BoxOf(number) + 1);
        }
        else
        {
            Box[number] = 0;
            Wrong[number] = Wrong.GetValueOrDefault(number) + 1;
        }
    }

    /// <summary>Records the chosen answer to a question: right if it is the correct one.</summary>
    public void RecordAnswer(Question question, int answer) => Record(question.Number, answer == question.Correct);

    public int BoxOf(int number) => Box.GetValueOrDefault(number);

    public bool IsNew(int number) => !Box.ContainsKey(number);

    public int CountMastered(IEnumerable<Question> questions) =>
        questions.Count(q => Box.TryGetValue(q.Number, out var box) && box >= MasteredBox);

    /// <summary>How many questions are in the <see cref="MistakeQueue"/>.</summary>
    public int CountMistakes(IEnumerable<Question> questions) => questions.Count(IsMistake);

    /// <summary>Questions answered wrong at least once and not yet learned: lowest box first, random within a box.</summary>
    public IReadOnlyList<int> MistakeQueue(IEnumerable<Question> questions, Random random) =>
        [.. questions
            .Where(IsMistake)
            .Shuffled(random)
            .OrderBy(q => BoxOf(q.Number)) // stable: keeps the random order within a box
            .Select(q => q.Number)];

    /// <summary>Starts a practice run over the questions in the mode's order.</summary>
    public void StartPractice(PracticeMode mode, IEnumerable<Question> questions, Random random)
    {
        var numbers = questions.Select(q => q.Number);
        int[] order = mode switch
        {
            PracticeMode.Random => numbers.Shuffled(random),
            PracticeMode.Unseen => numbers.Where(IsNew).Shuffled(random),
            _ => [.. numbers],
        };
        Practice = new PracticeRun(mode, order, 0);
    }

    /// <summary>Starts a practice run in catalogue order (<see cref="PracticeMode.Order"/>) at the question with this number.</summary>
    /// <returns>false if the question isn't among <paramref name="questions"/>; the current run stays.</returns>
    public bool StartPracticeAt(int number, IEnumerable<Question> questions)
    {
        int[] order = [.. questions.Select(q => q.Number)];
        var position = Array.IndexOf(order, number);
        if (position < 0) return false;
        Practice = new PracticeRun(PracticeMode.Order, order, position);
        return true;
    }

    /// <summary>How many questions are in each box; <c>New</c> = never answered.</summary>
    public BoxCounts CountBoxes(IEnumerable<Question> questions)
    {
        var inBox = new int[MasteredBox + 1];
        var unseen = 0;
        foreach (var q in questions)
        {
            if (Box.TryGetValue(q.Number, out var box)) inBox[box]++;
            else unseen++;
        }
        return new BoxCounts(unseen, inBox);
    }

    private bool IsMistake(Question q) => Wrong.ContainsKey(q.Number) && BoxOf(q.Number) < MasteredBox;

    /// <summary>Starts a new exam, replacing one in progress.</summary>
    public Exam StartExam(IEnumerable<Question> questions, DateTimeOffset now, Random random) =>
        Exam = Exam.Create(questions, now, random);

    /// <summary>Drops the exam in progress if it was abandoned (see <see cref="Exam.IsAbandoned"/>).</summary>
    public void DropAbandonedExam(DateTimeOffset now)
    {
        if (Exam?.IsAbandoned(now) == true) Exam = null;
    }

    /// <summary>
    /// Scores the exam in progress, records its answers in the learning progress and keeps the result.
    /// Unanswered questions count as wrong for the score but are not recorded as mistakes.
    /// </summary>
    public ExamResult FinishExam(Catalog catalog, DateTimeOffset now)
    {
        var exam = Exam ?? throw new InvalidOperationException("No exam in progress.");
        var wrong = new List<int>();
        foreach (var (number, answer) in exam.Questions.Zip(exam.Answers))
        {
            var correct = answer == catalog[number].Correct;
            if (!correct) wrong.Add(number);
            if (answer is not null) Record(number, correct);
        }
        var result = new ExamResult(now, exam.Questions, exam.Answers, wrong);
        Exams = [result, .. Exams.Take(ExamResult.Kept - 1)];
        Exam = null;
        return result;
    }
}

/// <param name="New">Never answered.</param>
/// <param name="InBox">Count per Leitner box, index = box.</param>
public sealed record BoxCounts(int New, IReadOnlyList<int> InBox);

internal static class ShuffleExtensions
{
    public static T[] Shuffled<T>(this IEnumerable<T> items, Random random)
    {
        var array = items.ToArray();
        random.Shuffle(array);
        return array;
    }
}
