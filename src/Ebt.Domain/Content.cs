using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ebt.Domain;

// The app's content as the pipeline writes it to wwwroot/data and the app reads it.
// The short JSON names keep the files small; the C# names say what they mean.

/// <summary>A piece of displayed text: underlined (emphasis from the official catalog) and/or a glossary term.</summary>
/// <remarks>Serialized as a plain JSON string when it is neither, to keep the data small.</remarks>
[JsonConverter(typeof(SegmentConverter))]
public sealed record Segment(string Text, bool Underline = false, int? Term = null);

/// <param name="Number">Catalog number: 1–300 general, 301–310 Nordrhein-Westfalen.</param>
/// <param name="Answers">Four answers.</param>
/// <param name="Correct">Index of the correct answer.</param>
/// <param name="Image">Picture shown with the question, relative to the app's base address.</param>
/// <param name="IsState">State-specific (Nordrhein-Westfalen) question.</param>
public sealed record Question(
    [property: JsonPropertyName("n")] int Number,
    [property: JsonPropertyName("q")] IReadOnlyList<Segment> Text,
    [property: JsonPropertyName("a")] IReadOnlyList<IReadOnlyList<Segment>> Answers,
    [property: JsonPropertyName("c")] int Correct,
    [property: JsonPropertyName("img")] string? Image = null,
    [property: JsonPropertyName("state"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool IsState = false)
{
    /// <summary>Questions 301–310 are the state-specific (Nordrhein-Westfalen) questions.</summary>
    public const int FirstStateNumber = 301;

    /// <summary>The number as printed in the official catalog: state questions count from 1 again.</summary>
    [JsonIgnore]
    public int CatalogNumber => IsState ? Number - FirstStateNumber + 1 : Number;
}

/// <summary>Russian translation of a question and its answers. Plain text and emphasis only, no glossary terms.</summary>
public sealed record Translation(
    [property: JsonPropertyName("q")] IReadOnlyList<Segment> Text,
    [property: JsonPropertyName("a")] IReadOnlyList<IReadOnlyList<Segment>> Answers);

/// <summary>Glossary entry; its index in the glossary is what <see cref="Segment.Term"/> refers to.</summary>
public sealed record Term(string De, string Ru, string Note)
{
    /// <summary>
    /// Sort key for German dictionary order (DIN 5007-1: ä = a, ß = ss), by the noun rather than the article,
    /// so "der Bundestag" is listed under B. Culture-free on purpose: the app runs without ICU data.
    /// </summary>
    [JsonIgnore]
    public string SortKey
    {
        get
        {
            var s = De;
            foreach (var article in (string[])["der / die ", "der ", "die ", "das "])
                if (s.StartsWith(article, StringComparison.Ordinal)) { s = s[article.Length..]; break; }
            return s.ToLowerInvariant()
                .Replace('ä', 'a').Replace('ö', 'o').Replace('ü', 'u').Replace("ß", "ss");
        }
    }
}

/// <summary>Everything the app shows: questions, glossary and Russian translations.</summary>
public sealed class Catalog(IReadOnlyList<Question> questions, IReadOnlyList<Term> terms, IReadOnlyDictionary<int, Translation> ru)
{
    private readonly Dictionary<int, Question> _byNumber = questions.ToDictionary(q => q.Number);

    public IReadOnlyList<Question> Questions { get; } = questions;
    public IReadOnlyList<Term> Terms { get; } = terms;
    public IReadOnlyDictionary<int, Translation> Ru { get; } = ru;

    public Question this[int number] => _byNumber[number];
    public bool Contains(int number) => _byNumber.ContainsKey(number);
}

internal sealed class SegmentConverter : JsonConverter<Segment>
{
    public override Segment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return new Segment(reader.GetString()!);
        using var doc = JsonDocument.ParseValue(ref reader);
        var o = doc.RootElement;
        return new Segment(
            o.GetProperty("t").GetString()!,
            o.TryGetProperty("u", out var u) && u.GetBoolean(),
            o.TryGetProperty("term", out var term) ? term.GetInt32() : null);
    }

    public override void Write(Utf8JsonWriter writer, Segment value, JsonSerializerOptions options)
    {
        if (!value.Underline && value.Term is null)
        {
            writer.WriteStringValue(value.Text);
            return;
        }
        writer.WriteStartObject();
        writer.WriteString("t", value.Text);
        if (value.Underline) writer.WriteBoolean("u", true);
        if (value.Term is { } term) writer.WriteNumber("term", term);
        writer.WriteEndObject();
    }
}
