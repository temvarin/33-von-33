using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ebt.Domain;

/// <summary>Where progress is kept (the browser's localStorage in the app, memory in tests).</summary>
public interface IKeyValueStore
{
    string? Read(string key);
    void Write(string key, string value);
}

/// <summary>
/// Persists the user's progress. The schema number marks the stored shape: when it changes,
/// add a migration from the previous schema here instead of discarding the user's progress.
/// </summary>
public sealed class ProgressStore(IKeyValueStore store)
{
    private const string Key = "ebt.progress";
    private const int Schema = 2; // 1 was the JavaScript version of the app

    public Progress Load()
    {
        try
        {
            var json = store.Read(Key);
            var stored = json is null ? null : JsonSerializer.Deserialize(json, ProgressJson.Default.StoredProgress);
            return stored is { Schema: Schema, Progress: { } progress } ? progress : new Progress();
        }
        catch (JsonException)
        {
            return new Progress(); // unreadable – start fresh rather than crash
        }
    }

    public void Save(Progress progress) =>
        store.Write(Key, JsonSerializer.Serialize(new StoredProgress(Schema, progress), ProgressJson.Default.StoredProgress));
}

internal sealed record StoredProgress(int Schema, Progress Progress);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(StoredProgress))]
internal sealed partial class ProgressJson : JsonSerializerContext;
