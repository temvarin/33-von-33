using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Ebt.Content;

/// <summary>
/// The app's data files and their JSON form, defined once: the pipeline writes them with these type infos and the
/// app reads them with the same ones. Source-generated: no reflection, so it survives trimming in the WebAssembly build.
/// </summary>
public static class ContentJson
{
    /// <summary>File names, relative to the data folder (wwwroot/data in the app).</summary>
    public const string QuestionsFile = "questions.json", TranslationsFile = "ru.json", TermsFile = "terms.json";

    private static readonly JsonSerializerOptions Options = new(ContentJsonContext.Default.Options)
    {
        // Keep Cyrillic, umlauts and „quotes“ readable in the files instead of \uXXXX escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static JsonTypeInfo<Question[]> Questions { get; } = TypeInfo<Question[]>();

    /// <summary>Russian translations by question number, written in number order.</summary>
    public static JsonTypeInfo<SortedDictionary<int, Translation>> Translations { get; } = TypeInfo<SortedDictionary<int, Translation>>();

    public static JsonTypeInfo<Term[]> Terms { get; } = TypeInfo<Term[]>();

    private static JsonTypeInfo<T> TypeInfo<T>() => (JsonTypeInfo<T>)Options.GetTypeInfo(typeof(T));
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Question[]))]
[JsonSerializable(typeof(SortedDictionary<int, Translation>))]
[JsonSerializable(typeof(Term[]))]
internal sealed partial class ContentJsonContext : JsonSerializerContext;
