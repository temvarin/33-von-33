namespace Ebt.Learning;

// Schema 2: the stored shape of the progress, as released. These records are the storage format only, kept apart
// from the domain types so that renaming or reshaping a domain type cannot change what is read from the phone.
// Never change them: a new shape is a new schema with its own records and a migration from this one.

/// <param name="Box">Leitner box per question number.</param>
/// <param name="Wrong">How often each question was answered wrong.</param>
internal sealed record ProgressV2(
    Dictionary<int, int>? Box,
    Dictionary<int, int>? Wrong,
    PracticeRunV2? Practice,
    ExamV2? Exam,
    ExamResultV2[]? Exams);

internal enum PracticeModeV2 { Order, Random, Unseen }

internal sealed record PracticeRunV2(PracticeModeV2 Mode, int[]? Order, int Position);

internal sealed record ExamV2(DateTimeOffset Started, int[]? Questions, int?[]? Answers, int Position);

internal sealed record ExamResultV2(DateTimeOffset Date, int[]? Questions, int?[]? Answers, int[]? Wrong);

/// <summary>Maps between the domain and schema 2. Missing parts load as empty, as they always did.</summary>
internal static class ProgressSchema2
{
    public static ProgressV2 From(Progress progress) => new(
        new Dictionary<int, int>(progress.Box),
        new Dictionary<int, int>(progress.Wrong),
        new PracticeRunV2(From(progress.Practice.Mode), [.. progress.Practice.Order], progress.Practice.Position),
        progress.Exam is { } exam ? new ExamV2(exam.Started, [.. exam.Questions], [.. exam.Answers], exam.Position) : null,
        [.. progress.Exams.Select(r => new ExamResultV2(r.Date, [.. r.Questions], [.. r.Answers], [.. r.Wrong]))]);

    public static Progress ToProgress(ProgressV2 stored) => new(
        stored.Box ?? [],
        stored.Wrong ?? [],
        stored.Practice is { } run ? new PracticeRun(ToMode(run.Mode), run.Order ?? [], run.Position) : PracticeRun.None,
        stored.Exam is { } exam ? new Exam(exam.Started, exam.Questions ?? [], exam.Answers ?? [], exam.Position) : null,
        [.. (stored.Exams ?? []).Select(r => new ExamResult(r.Date, r.Questions ?? [], r.Answers ?? [], r.Wrong ?? []))]);

    // Spelled out instead of cast: the stored names must not follow a renamed domain enum.
    private static PracticeModeV2 From(PracticeMode mode) => mode switch
    {
        PracticeMode.Order => PracticeModeV2.Order,
        PracticeMode.Random => PracticeModeV2.Random,
        PracticeMode.Unseen => PracticeModeV2.Unseen,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "No stored form for this practice mode."),
    };

    private static PracticeMode ToMode(PracticeModeV2 mode) => mode switch
    {
        PracticeModeV2.Order => PracticeMode.Order,
        PracticeModeV2.Random => PracticeMode.Random,
        PracticeModeV2.Unseen => PracticeMode.Unseen,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown stored practice mode."),
    };
}
