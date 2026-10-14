using Ebt.Domain;

namespace Ebt.App.Services;

/// <summary>The user's progress for the running app. Components change <see cref="Progress"/>, then call <see cref="Save"/>.</summary>
public sealed class AppState(ProgressStore store)
{
    public Progress Progress { get; private set; } = store.Load();

    public void Save() => store.Save(Progress);

    /// <summary>Replaces the progress with an empty one.</summary>
    public void Reset()
    {
        Progress = new Progress();
        Save();
    }
}
