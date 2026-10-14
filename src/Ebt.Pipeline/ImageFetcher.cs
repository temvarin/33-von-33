using System.Buffers.Binary;

namespace Ebt.Pipeline;

/// <summary>
/// Downloads the official question images from oet.bamf.de into a local cache. Most only show the question text
/// (transcribed into questions-de.txt); taller ones contain a picture (coat of arms, map, photo) and are copied to
/// content/pictures, from where <see cref="DataBuilder"/> ships them with the app.
/// </summary>
internal sealed class ImageFetcher(HttpClient http, Paths paths)
{
    private const string BaseUrl =
        "https://oet.bamf.de/ords/oetut/f?p=514:30::APPLICATION_PROCESS%3Dshow_pag_bild:::F514_PAG_THUMBNAIL,F514_PAG_BILD:,";

    /// <summary>Text-only images are at most two or three lines high; anything taller contains a picture.</summary>
    internal const int PictureMinHeight = 110;

    /// <returns>The number of picture questions.</returns>
    public async Task<int> FetchAsync(IEnumerable<BamfAnswer> answers)
    {
        Directory.CreateDirectory(paths.ImageCache);
        var pictures = 0;
        foreach (var answer in answers.Where(a => !a.NoImg))
        {
            var cached = Path.Combine(paths.ImageCache, $"{answer.Id}.png");
            if (!File.Exists(cached) || !Png.IsPng(await File.ReadAllBytesAsync(cached)))
            {
                await File.WriteAllBytesAsync(cached, await DownloadAsync(answer.Id));
                await Task.Delay(150); // be gentle with the BAMF server
            }
            if (Png.Height(await File.ReadAllBytesAsync(cached)) > PictureMinHeight)
            {
                File.Copy(cached, Path.Combine(paths.Pictures, $"{answer.Id}.png"), overwrite: true);
                pictures++;
            }
        }
        return pictures;
    }

    private async Task<byte[]> DownloadAsync(int id)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var bytes = await http.GetByteArrayAsync(BaseUrl + id);
            if (Png.IsPng(bytes)) return bytes;
            Console.Error.WriteLine($"  image {id}: attempt {attempt} returned no image");
            await Task.Delay(1000 * attempt);
        }
        throw new InvalidDataException($"Image {id} could not be downloaded.");
    }
}

internal static class Png
{
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsPng(ReadOnlySpan<byte> data) => data.Length > 24 && data.StartsWith(Signature);

    /// <summary>Image height from the IHDR chunk header (bytes 20–23, big-endian).</summary>
    public static int Height(ReadOnlySpan<byte> data) => BinaryPrimitives.ReadInt32BigEndian(data[20..24]);
}
