namespace Ebt.Pipeline;

/// <summary>Where the pipeline reads its sources and writes the app's data, relative to the repository root.</summary>
public sealed record Paths(string Root)
{
    public string Sources => Path.Combine(Root, "content");
    public string Pictures => Path.Combine(Sources, "pictures");
    public string ImageCache => Path.Combine(Root, ".cache", "bamf-images");
    public string AppData => Path.Combine(Root, "src", "Ebt.App", "wwwroot", "data");

    /// <summary>Walks up from <paramref name="start"/> to the repository root: the folder that contains <c>content</c> and <c>Ebt.slnx</c>.</summary>
    public static Paths FindFrom(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Ebt.slnx")) && Directory.Exists(Path.Combine(dir.FullName, "content")))
                return new Paths(dir.FullName);
        }
        throw new DirectoryNotFoundException($"No repository root (with Ebt.slnx and content/) above {start}.");
    }
}
