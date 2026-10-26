namespace Ebt.Learning.Tests;

internal static class Fixtures
{
    public static readonly DateTimeOffset Now = new(2026, 10, 13, 10, 0, 0, TimeSpan.Zero);

    /// <summary>A catalog shaped like the real one: general questions 1… and state questions 301….</summary>
    public static Catalog Catalog(int general = 300, int state = 10) => new(
        [
            .. Enumerable.Range(1, general).Select(n => Question(n)),
            .. Enumerable.Range(Content.Question.FirstStateNumber, state).Select(n => Question(n, isState: true)),
        ],
        [],
        new Dictionary<int, Translation>());

    public static Question Question(int n, bool isState = false) =>
        new(n, [new Segment($"Frage {n}")], [[new("A")], [new("B")], [new("C")], [new("D")]], n % 4, IsState: isState);

    /// <summary>Same seed, same shuffles.</summary>
    public static Random Random() => new(42);
}
