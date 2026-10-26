using Ebt.App.Shared;
using Ebt.Content;
using Ebt.Learning;

namespace Ebt.App.Features.Review;

/// <summary>Reviewing mistakes: the weakest question first, until every mistake is learned. Answers are saved at once.</summary>
public sealed class ReviewSession(AppState state, Catalog catalog, Random random)
{
    /// <summary>The questions still to review, the one to ask next first (see <see cref="Progress.MistakeQueue"/>).</summary>
    public IReadOnlyList<int> Queue() => state.Progress.MistakeQueue(catalog.Questions, random);

    public void Answer(Question question, int answer) => state.Change(p => p.RecordAnswer(question, answer));
}
