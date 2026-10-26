using Ebt.Learning;

namespace Ebt.App.Shared;

/// <summary>
/// The user's progress for the running app. Pages read <see cref="Progress"/>; changes go through each feature's
/// session service (practice, review, mock exam), which uses <see cref="Change"/>, so every change is saved in the
/// same call. The architecture tests check that pages don't change it themselves.
/// </summary>
public sealed class AppState(ProgressStore store)
{
    public Progress Progress { get; private set; } = store.Load();

    /// <summary>Changes the progress and saves it.</summary>
    internal void Change(Action<Progress> change)
    {
        change(Progress);
        store.Save(Progress);
    }

    /// <inheritdoc cref="Change(Action{Progress})"/>
    internal T Change<T>(Func<Progress, T> change)
    {
        var result = change(Progress);
        store.Save(Progress);
        return result;
    }

    /// <summary>Replaces the progress with an empty one.</summary>
    public void Reset()
    {
        Progress = new Progress();
        store.Save(Progress);
    }
}
