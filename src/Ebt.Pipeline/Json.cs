using System.Text.Json;

namespace Ebt.Pipeline;

/// <summary>
/// Reads the hand-maintained sources in content/. Their format is the pipeline's own; the app's data is written
/// with the contract's serializer (<see cref="Ebt.Content.ContentJson"/>).
/// </summary>
internal static class Json
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
        ?? throw new InvalidDataException($"{path} is empty.");
}
