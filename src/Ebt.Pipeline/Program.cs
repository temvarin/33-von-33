using Ebt.Pipeline;

var paths = Paths.FindFrom(Directory.GetCurrentDirectory());

return args switch
{
    ["build"] => Commands.Build(paths),
    ["check"] => Commands.Check(paths),
    ["fetch-images"] => await Commands.FetchImagesAsync(paths),
    ["translation-report"] => Commands.TranslationReport(paths),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("""
        Usage: dotnet run --project src/Ebt.Pipeline -- <command>

          build               validate content/ and write the app's wwwroot/data
          check               validate and verify wwwroot/data is up to date (CI)
          fetch-images        download the official question images; pictures go to content/pictures
          translation-report  list translations that don't use the glossary's Russian term (manual review)
        """);
    return 2;
}
