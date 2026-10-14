using System.Globalization;

namespace Ebt.Pipeline;

/// <summary>A question's answers as captured from the official BAMF practice site (oet.bamf.de, NRW).</summary>
/// <param name="Nr">Catalog number: 1–300 general, 301–310 Nordrhein-Westfalen.</param>
/// <param name="Id">BAMF's internal id, used in the image URL.</param>
/// <param name="Correct">Index (0–3) of the officially correct answer.</param>
/// <param name="NoImg">The question text is plain HTML on the BAMF site, there is no image.</param>
public sealed record BamfAnswer(int Nr, int Id, int Correct, string[] Answers, bool NoImg = false);

/// <summary>Russian translation of a question and its four answers. "_word_" marks emphasis.</summary>
public sealed record Translation(string Q, string[] A);

/// <summary>Glossary entry. <paramref name="Forms"/> are the German word forms highlighted in the questions.</summary>
public sealed record GlossaryTerm(string De, string[] Forms, string Ru, string Note);

/// <summary>All hand-maintained inputs of the pipeline (content/).</summary>
public sealed record SourceData(
    IReadOnlyList<BamfAnswer> Answers,
    IReadOnlyDictionary<int, string> Texts,
    IReadOnlyDictionary<int, Translation> Translations,
    IReadOnlyList<GlossaryTerm> Glossary,
    IReadOnlySet<int> PictureIds)
{
    public static SourceData Load(Paths paths) => new(
        Json.Read<List<BamfAnswer>>(Path.Combine(paths.Sources, "bamf-answers.json")),
        ParseTexts(File.ReadAllLines(Path.Combine(paths.Sources, "questions-de.txt"))),
        Json.Read<Dictionary<int, Translation>>(Path.Combine(paths.Sources, "questions-ru.json")),
        Json.Read<List<GlossaryTerm>>(Path.Combine(paths.Sources, "glossary.json")),
        Directory.GetFiles(paths.Pictures, "*.png").Select(f => int.Parse(Path.GetFileNameWithoutExtension(f), CultureInfo.InvariantCulture)).ToHashSet());

    /// <summary>Parses "nr|text" lines – the question texts transcribed from the official question images.</summary>
    internal static Dictionary<int, string> ParseTexts(IEnumerable<string> lines)
    {
        var texts = new Dictionary<int, string>();
        var lineNo = 0;
        foreach (var line in lines)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var sep = line.IndexOf('|');
            if (sep < 1 || !int.TryParse(line[..sep], CultureInfo.InvariantCulture, out var nr))
                throw new FormatException($"questions-de.txt line {lineNo}: expected \"nr|text\".");
            if (!texts.TryAdd(nr, line[(sep + 1)..].Trim()))
                throw new FormatException($"questions-de.txt line {lineNo}: question {nr} appears twice.");
        }
        return texts;
    }
}
