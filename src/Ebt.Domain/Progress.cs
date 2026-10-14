using System.Text.Json.Serialization;

namespace Ebt.Domain;

public enum PracticeMode { Order, Random, Unseen }

/// <summary>Where the user is in a practice run.</summary>
public sealed class PracticeRun
{
    [JsonInclude] public PracticeMode Mode { get; internal set; }
    [JsonInclude] public IReadOnlyList<int> Order { get; internal set; } = [];

    /// <summary>Index into <see cref="Order"/>; equal to its length when the run is finished.</summary>
    [JsonInclude] public int Position { get; internal set; }

    [JsonIgnore] public bool IsFinished => Position >= Order.Count;

    /// <summary>The number of the question at the current position.</summary>
    [JsonIgnore] public int Current => Order[Position];

    public void Next() => Position = Math.Min(Position + 1, Order.Count);

    public void Previous() => Position = Math.Max(Position - 1, 0);
}

/// <summary>
/// The user's learning progress as a Leitner system: a correct answer moves a question up one box,
/// a wrong one back to box 0. A question in the top box counts as learned. Persisted by <see cref="ProgressStore"/>.
/// </summary>
/// <remarks>State changes only through its methods; the internal setters exist for deserialization.</remarks>
public sealed class Progress
{
    public const int MasteredBox = 3;

    /// <summary>Leitner box (0–3) per question number; absent = never answered.</summary>
    [JsonInclude] internal Dictionary<int, int> Box { get; set; } = [];

    /// <summary>How often each question was answered wrong.</summary>
    [JsonInclude] internal Dictionary<int, int> Wrong { get; set; } = [];

    [JsonInclude] public PracticeRun Practice { get; internal set; } = new();

    /// <summary>The exam in progress, if any.</summary>
    [JsonInclude] public Exam? Exam { get; internal set; }

    /// <summary>Finished exams, newest first.</summary>
    [JsonInclude] public IReadOnlyList<ExamResult> Exams { get; internal set; } = [];

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

    public int BoxOf(int number) => Box.GetValueOrDefault(number);

    public bool IsNew(int number) => !Box.ContainsKey(number);

    public int CountMastered(IEnumerable<Question> questions) =>
        questions.Count(q => Box.TryGetValue(q.Number, out var box) && box >= MasteredBox);

    /// <summary>Questions answered wrong at least once and not yet learned: lowest box first, random within a box.</summary>
    public IReadOnlyList<int> MistakeQueue(IEnumerable<Question> questions, Random random) =>
        [.. questions
            .Where(q => Wrong.ContainsKey(q.Number) && BoxOf(q.Number) < MasteredBox)
            .Shuffled(random)
            .OrderBy(q => BoxOf(q.Number)) // stable: keeps the random order within a box
            .Select(q => q.Number)];

    /// <summary>Starts a practice run over the questions in the mode's order.</summary>
    /// <param name="position">Where to start, e.g. at a question chosen by number.</param>
    public void StartPractice(PracticeMode mode, IEnumerable<Question> questions, Random random, int position = 0)
    {
        var numbers = questions.Select(q => q.Number);
        int[] order = mode switch
        {
            PracticeMode.Random => numbers.Shuffled(random),
            PracticeMode.Unseen => numbers.Where(IsNew).Shuffled(random),
            _ => [.. numbers],
        };
        Practice = new PracticeRun { Mode = mode, Order = order, Position = Math.Clamp(position, 0, order.Length) };
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

    /// <summary>Starts a new exam, replacing one in progress.</summary>
    public Exam StartExam(IEnumerable<Question> questions, DateTimeOffset now, Random random) =>
        Exam = Exam.Create(questions, now, random);

    /// <summary>Drops the exam in progress if it was abandoned (see <see cref="Exam.IsAbandoned"/>).</summary>
    /// <returns>Whether the progress changed.</returns>
    public bool DropAbandonedExam(DateTimeOffset now)
    {
        if (Exam?.IsAbandoned(now) != true) return false;
        Exam = null;
        return true;
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
