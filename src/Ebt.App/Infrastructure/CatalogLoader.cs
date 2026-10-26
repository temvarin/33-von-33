using System.Net.Http.Json;
using System.Text.Json;
using Ebt.Content;

namespace Ebt.App.Infrastructure;

/// <summary>Loads the content written by the pipeline (wwwroot/data).</summary>
internal static class CatalogLoader
{
    /// <summary>Where the pipeline's files are, relative to the app's base address.</summary>
    private const string Folder = "data/";

    /// <returns>null if the data can't be loaded – the app was never opened online, or a release is incomplete.</returns>
    public static async Task<Catalog?> TryLoadAsync(HttpClient http)
    {
        try
        {
            var questions = http.GetFromJsonAsync(Folder + ContentJson.QuestionsFile, ContentJson.Questions);
            var terms = http.GetFromJsonAsync(Folder + ContentJson.TermsFile, ContentJson.Terms);
            var ru = http.GetFromJsonAsync(Folder + ContentJson.TranslationsFile, ContentJson.Translations);
            return new Catalog(await questions ?? [], await terms ?? [], await ru ?? []);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException)
        {
            Console.Error.WriteLine($"Loading the catalog failed: {e.Message}");
            return null;
        }
    }
}
