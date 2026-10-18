using System.Diagnostics.CodeAnalysis;

namespace Ebt.Pipeline;

/// <summary>The command-line entry points. Each returns the process exit code.</summary>
internal static class Commands
{
    /// <summary>Validates the sources and writes wwwroot/data.</summary>
    public static int Build(Paths paths)
    {
        if (!TryBuild(paths, out var output)) return 1;

        Directory.CreateDirectory(paths.AppData);
        foreach (var (name, json) in output.JsonFiles)
            File.WriteAllText(Path.Combine(paths.AppData, name), json);

        var img = Path.Combine(paths.AppData, "img");
        if (Directory.Exists(img)) Directory.Delete(img, recursive: true);
        Directory.CreateDirectory(img);
        foreach (var id in output.PictureIds)
            File.Copy(Path.Combine(paths.Pictures, $"{id}.png"), Path.Combine(img, $"{id}.png"));

        Console.WriteLine($"wwwroot/data: {output.JsonFiles.Count} files, {output.PictureIds.Count} pictures");
        return 0;
    }

    /// <summary>Validates the sources and verifies that wwwroot/data is exactly what <see cref="Build"/> would write (used in CI).</summary>
    public static int Check(Paths paths)
    {
        if (!TryBuild(paths, out var output)) return 1;

        var stale = new List<string>();
        foreach (var (name, json) in output.JsonFiles)
        {
            var file = Path.Combine(paths.AppData, name);
            if (!File.Exists(file) || File.ReadAllText(file) != json) stale.Add($"wwwroot/data/{name}");
        }

        var img = Path.Combine(paths.AppData, "img");
        var expected = output.PictureIds.Select(id => $"{id}.png").ToHashSet();
        var actual = Directory.Exists(img) ? Directory.GetFiles(img).Select(Path.GetFileName).ToHashSet() : [];
        stale.AddRange(actual.Except(expected).Select(f => $"wwwroot/data/img/{f} (not a picture question)"));
        foreach (var name in expected)
        {
            var target = Path.Combine(img, name);
            if (!File.Exists(target) || !File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(Path.Combine(paths.Pictures, name))))
                stale.Add($"wwwroot/data/img/{name}");
        }

        if (stale.Count > 0)
        {
            Console.Error.WriteLine("wwwroot/data is out of date – run `build`:\n  " + string.Join("\n  ", stale));
            return 1;
        }
        Console.WriteLine("Sources valid, wwwroot/data up to date.");
        return 0;
    }

    public static async Task<int> FetchImagesAsync(Paths paths)
    {
        var src = SourceData.Load(paths);
        using var http = new HttpClient();
        var pictures = await new ImageFetcher(http, paths).FetchAsync(src.Answers);
        Console.WriteLine($"{pictures} picture questions in content/pictures – run `build` to update the app.");
        return 0;
    }

    public static int TranslationReport(Paths paths)
    {
        foreach (var line in Pipeline.TranslationReport.Find(SourceData.Load(paths)))
            Console.WriteLine(line);
        return 0;
    }

    private static bool TryBuild(Paths paths, [NotNullWhen(true)] out BuildOutput? output)
    {
        var src = SourceData.Load(paths);
        var problems = Validation.Check(src, CatalogShape.Nrw);
        if (problems.Count > 0)
        {
            Console.Error.WriteLine($"{problems.Count} problem(s) in content/:\n  " + string.Join("\n  ", problems));
            output = null;
            return false;
        }
        output = DataBuilder.Build(src);
        return true;
    }
}
