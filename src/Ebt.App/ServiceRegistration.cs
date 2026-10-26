using Ebt.App.Features.MockExam;
using Ebt.App.Features.Practice;
using Ebt.App.Features.Review;
using Ebt.App.Infrastructure;
using Ebt.App.Shared;
using Ebt.Content;
using Ebt.Learning;

namespace Ebt.App;

public static class ServiceRegistration
{
    /// <summary>Everything the components use; the one place that knows every module. Clock and randomness are services too, so tests can replace them.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services, Catalog catalog) => services
        .AddSingleton(catalog)
        .AddSingleton<IKeyValueStore, LocalStorage>()
        .AddSingleton<BrowserWindow>()
        .AddSingleton<ProgressStore>()
        .AddSingleton<AppState>()
        .AddSingleton<PracticeSession>()
        .AddSingleton<ReviewSession>()
        .AddSingleton<ExamSession>()
        .AddSingleton<Dialogs>()
        .AddSingleton(TimeProvider.System)
        .AddSingleton(Random.Shared);
}
