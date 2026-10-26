using Ebt.App.Shared;
using Ebt.Content;
using Ebt.Learning;

namespace Ebt.App.Features.Practice;

/// <summary>Practising the catalogue: starting a run, answering, moving through it. Every change is saved at once.</summary>
public sealed class PracticeSession(AppState state, Catalog catalog, Random random)
{
    public PracticeRun Run => state.Progress.Practice;

    public void Start(PracticeMode mode) => state.Change(p => p.StartPractice(mode, catalog.Questions, random));

    /// <summary>Starts a run in catalogue order at the question with this number.</summary>
    /// <returns>false if there is no such question; the current run stays.</returns>
    public bool StartAt(int number) => state.Change(p => p.StartPracticeAt(number, catalog.Questions));

    /// <returns>Whether the answer was correct.</returns>
    public bool Answer(Question question, int answer) =>
        state.Change(p => p.RecordAnswer(question, answer));

    public void Next() => state.Change(p => p.Practice.Next());

    public void Previous() => state.Change(p => p.Practice.Previous());
}
