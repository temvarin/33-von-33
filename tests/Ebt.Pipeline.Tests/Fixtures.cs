namespace Ebt.Pipeline.Tests;

/// <summary>A minimal, valid set of sources; tests change one thing and expect exactly that to be reported.</summary>
internal static class Fixtures
{
    /// <summary>The catalogue <see cref="Valid"/> sources make up: question 1 only.</summary>
    public static readonly CatalogShape Shape = new(General: 1, State: 0);

    public static SourceData Valid(
        IReadOnlyList<BamfAnswer>? answers = null,
        IReadOnlyDictionary<int, string>? texts = null,
        IReadOnlyDictionary<int, Translation>? translations = null,
        IReadOnlyList<GlossaryTerm>? glossary = null,
        IReadOnlySet<int>? pictures = null) => new(
        answers ?? [new BamfAnswer(1, 100, 1, ["Monarchie", "Republik", "Diktatur", "1949"])],
        texts ?? new Dictionary<int, string> { [1] = "Was für eine Staatsform hat Deutschland?" },
        translations ?? new Dictionary<int, Translation>
        {
            [1] = new("Какая форма государства в Германии?", ["монархия", "республика", "диктатура", "1949"]),
        },
        glossary ?? [new GlossaryTerm("die Republik", ["Republik"], "республика", "Государство с выборным главой.")],
        pictures ?? new HashSet<int>());

    public static Dictionary<int, Translation> WithAnswers(params string[] ru) =>
        new() { [1] = new("Какая форма государства в Германии?", ru) };
}
