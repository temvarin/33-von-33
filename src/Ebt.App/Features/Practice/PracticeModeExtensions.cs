using Ebt.Learning;

namespace Ebt.App.Features.Practice;

internal static class PracticeModeExtensions
{
    public static string Label(this PracticeMode mode) => mode switch
    {
        PracticeMode.Random => "Zufällig",
        PracticeMode.Unseen => "Nur neue",
        _ => "Der Reihe nach",
    };
}
