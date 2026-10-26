using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ebt.Learning;

/// <summary>Where progress is kept (the browser's localStorage in the app, memory in tests).</summary>
public interface IKeyValueStore
{
    string? Read(string key);
    void Write(string key, string value);
}

/// <summary>
/// Persists the user's progress. It is stored in its own records (<see cref="ProgressV2"/>), mapped to and from
/// the domain here, so the domain types can change without changing what is read from the phone. The schema
/// number marks the stored shape: progress with another schema is never misread, it starts fresh. There is no
/// migration yet because no released version stored another schema. When the shape changes after a release,
/// add records for the new schema and a migration from the released one instead of discarding the user's progress.
/// </summary>
public sealed class ProgressStore(IKeyValueStore store)
{
    private const string Key = "ebt.progress";
    private const int Schema = 2; // 1 was the unreleased JavaScript prototype

    public Progress Load()
    {
        try
        {
            var json = store.Read(Key);
            var stored = json is null ? null : JsonSerializer.Deserialize(json, ProgressJson.Default.StoredProgress);
            return stored is { Schema: Schema, Progress: { } progress } ? ProgressSchema2.ToProgress(progress) : new Progress();
        }
        catch (JsonException)
        {
            return new Progress(); // unreadable – start fresh rather than crash
        }
    }

    public void Save(Progress progress) =>
        store.Write(Key, JsonSerializer.Serialize(new StoredProgress(Schema, ProgressSchema2.From(progress)), ProgressJson.Default.StoredProgress));
}

/// <param name="Progress">In the shape of <paramref name="Schema"/>; read only when that is the current schema.</param>
internal sealed record StoredProgress(int Schema, ProgressV2? Progress);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(StoredProgress))]
internal sealed partial class ProgressJson : JsonSerializerContext;
