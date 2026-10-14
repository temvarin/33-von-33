using System.Text.Json.Serialization;

namespace Ebt.Domain;

/// <summary>A mock exam in the official format: 30 general + 3 state questions, 60 minutes, passed with 17 correct.</summary>
/// <remarks>State changes only through its methods; the internal setters exist for deserialization.</remarks>
public sealed class Exam
{
    public const int GeneralQuestions = 30;
    public const int StateQuestions = 3;
    public const int PassMark = 17;
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(60);

    [JsonConstructor]
    internal Exam() { }

    [JsonInclude] public DateTimeOffset Started { get; internal set; }

    /// <summary>Question numbers in exam order.</summary>
    [JsonInclude] public IReadOnlyList<int> Questions { get; internal set; } = [];

    /// <summary>Chosen answer per question; null = not answered.</summary>
    [JsonInclude] public IReadOnlyList<int?> Answers { get; internal set; } = [];

    /// <summary>The question currently shown.</summary>
    [JsonInclude] public int Position { get; internal set; }

    [JsonIgnore] public int AnsweredCount => Answers.Count(a => a is not null);

    internal static Exam Create(IEnumerable<Question> questions, DateTimeOffset now, Random random)
    {
        var all = questions.ToList();
        var general = all.Where(q => !q.IsState).Shuffled(random).Take(GeneralQuestions);
        var state = all.Where(q => q.IsState).Shuffled(random).Take(StateQuestions);
        var numbers = general.Concat(state).Select(q => q.Number).Shuffled(random);
        return new Exam { Started = now, Questions = numbers, Answers = new int?[numbers.Length] };
    }

    public TimeSpan Remaining(DateTimeOffset now) => Started + Duration - now;

    public bool IsOver(DateTimeOffset now) => Remaining(now) <= TimeSpan.Zero;

    /// <summary>Ran out of time without a single answer: the user left it, it isn't a result.</summary>
    public bool IsAbandoned(DateTimeOffset now) => IsOver(now) && Answers.All(a => a is null);

    public void GoTo(int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, Questions.Count);
        Position = position;
    }

    /// <summary>Answers the current question.</summary>
    /// <returns>The position to show next: the next unanswered question, if there is one after this.</returns>
    public int Answer(int answer)
    {
        var answers = Answers.ToArray();
        answers[Position] = answer;
        Answers = answers;
        var next = Array.FindIndex(answers, Position + 1, a => a is null);
        return next != -1 ? next : Math.Min(Position + 1, Questions.Count - 1);
    }
}

/// <param name="Questions">Question numbers in exam order.</param>
/// <param name="Answers">Chosen answer per question; null = not answered.</param>
/// <param name="Wrong">Question numbers answered wrong or not at all.</param>
public sealed record ExamResult(DateTimeOffset Date, IReadOnlyList<int> Questions, IReadOnlyList<int?> Answers, IReadOnlyList<int> Wrong)
{
    /// <summary>How many results are kept.</summary>
    public const int Kept = 50;

    [JsonIgnore] public int Total => Questions.Count;
    [JsonIgnore] public int Score => Total - Wrong.Count;
    [JsonIgnore] public bool Passed => Score >= Exam.PassMark;
}
