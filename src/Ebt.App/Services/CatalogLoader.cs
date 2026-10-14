using System.Net.Http.Json;
using System.Text.Json;
using Ebt.Domain;

namespace Ebt.App.Services;

/// <summary>Loads the content written by the pipeline (wwwroot/data).</summary>
internal static class CatalogLoader
{
    /// <returns>null if the data can't be loaded – the app was never opened online, or a release is incomplete.</returns>
    public static async Task<Catalog?> TryLoadAsync(HttpClient http)
    {
        try
        {
            var questions = http.GetFromJsonAsync("data/questions.json", DataJson.Default.QuestionArray);
            var terms = http.GetFromJsonAsync("data/terms.json", DataJson.Default.TermArray);
            var ru = http.GetFromJsonAsync("data/ru.json", DataJson.Default.DictionaryInt32Translation);
            return new Catalog(await questions ?? [], await terms ?? [], await ru ?? []);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException)
        {
            Console.Error.WriteLine($"Loading the catalog failed: {e.Message}");
            return null;
        }
    }
}
