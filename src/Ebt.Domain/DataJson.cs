using System.Text.Json.Serialization;

namespace Ebt.Domain;

/// <summary>
/// Compile-time generated JSON (de)serialization for the app's data:
/// no reflection, so it survives trimming in the WebAssembly build and starts faster.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Question[]))]
[JsonSerializable(typeof(Dictionary<int, Translation>))]
[JsonSerializable(typeof(Term[]))]
public sealed partial class DataJson : JsonSerializerContext;
