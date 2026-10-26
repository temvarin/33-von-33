using System.Text.Json;
using Ebt.Content;
using AppTranslation = Ebt.Content.Translation;

namespace Ebt.Pipeline;

/// <summary>Everything the app needs, built in memory: JSON files by name and the picture images to copy.</summary>
public sealed record BuildOutput(IReadOnlyDictionary<string, string> JsonFiles, IReadOnlyList<int> PictureIds);

/// <summary>
/// Turns validated sources into the app's data, written with the contract's own types and JSON form (<see cref="ContentJson"/>).
/// Pure: no file access, so build and check share it.
/// </summary>
public static class DataBuilder
{
    public static BuildOutput Build(SourceData src)
    {
        var terms = new TermMatcher(src.Glossary);
        var answers = src.Answers.OrderBy(a => a.Nr).ToList();

        Question[] questions = [.. answers.Select(a => new Question(
            a.Nr,
            RichText.Parse(src.Texts[a.Nr], terms),
            [.. a.Answers.Select(x => RichText.Parse(x, terms))],
            a.Correct,
            src.PictureIds.Contains(a.Id) ? $"data/img/{a.Id}.png" : null,
            a.Nr >= Question.FirstStateNumber))];

        // Translations get emphasis segments only: the glossary explains German terms.
        var translations = new SortedDictionary<int, AppTranslation>(answers.ToDictionary(
            a => a.Nr,
            a => new AppTranslation(
                RichText.Parse(src.Translations[a.Nr].Q),
                [.. src.Translations[a.Nr].A.Select(x => RichText.Parse(x))])));

        return new BuildOutput(
            new Dictionary<string, string>
            {
                [ContentJson.QuestionsFile] = JsonSerializer.Serialize(questions, ContentJson.Questions),
                [ContentJson.TranslationsFile] = JsonSerializer.Serialize(translations, ContentJson.Translations),
                [ContentJson.TermsFile] = JsonSerializer.Serialize([.. src.Glossary.Select(t => new Term(t.De, t.Ru, t.Note))], ContentJson.Terms),
            },
            [.. answers.Where(a => src.PictureIds.Contains(a.Id)).Select(a => a.Id).Order()]);
    }
}
