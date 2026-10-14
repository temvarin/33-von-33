namespace Ebt.Pipeline.Tests;

public class SourceParsingTests
{
    [Fact]
    public void Parses_question_texts_and_skips_blank_lines()
    {
        var texts = SourceData.ParseTexts(["1|Wie heißt die deutsche Verfassung?", "", "301|Welches Wappen gehört zu NRW? "]);
        Assert.Equal(new Dictionary<int, string> { [1] = "Wie heißt die deutsche Verfassung?", [301] = "Welches Wappen gehört zu NRW?" }, texts);
    }

    [Fact]
    public void Rejects_a_question_number_that_appears_twice()
    {
        var error = Assert.Throws<FormatException>(() => SourceData.ParseTexts(["1|a", "1|b"]));
        Assert.Contains("line 2", error.Message);
    }

    [Fact]
    public void Rejects_a_line_without_number() =>
        Assert.Throws<FormatException>(() => SourceData.ParseTexts(["Wie heißt die Verfassung?"]));

    [Fact]
    public void Reads_png_signature_and_height()
    {
        byte[] header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R',
                         0, 0, 2, 20, 0, 0, 0, 46, 8, 2];
        Assert.True(Png.IsPng(header));
        Assert.Equal(46, Png.Height(header));
        Assert.False(Png.IsPng([]));
    }
}
