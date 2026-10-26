using Ebt.App.Shared;
using Ebt.Content;
using Ebt.Learning;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Ebt.App.Tests;

/// <summary>The app's real services, with a small catalog, progress in memory and a clock the test controls.</summary>
public abstract class AppTestContext : BunitContext
{
    protected static readonly Term Steuern = new("die Steuern", "налоги", "Обязательные платежи государству.");

    /// <summary>Question 1 has the glossary term "Steuern" in answer B and a Russian translation; 301 is a state question.</summary>
    internal static readonly Catalog Catalog = new(
        [
            .. Enumerable.Range(1, 40).Select(n => Question(n)),
            .. Enumerable.Range(Content.Question.FirstStateNumber, 5).Select(n => Question(n, isState: true)),
        ],
        [Steuern],
        new Dictionary<int, Translation>
        {
            [1] = new([new("Вопрос 1")], [[new("A")], [new("налоги")], [new("C")], [new("D")]]),
        });

    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));

    protected AppTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose; // scrolling to the top is the only JavaScript call left
        Services.AddAppServices(Catalog);
        Services.AddSingleton<IKeyValueStore, MemoryStore>();
        Services.AddSingleton<TimeProvider>(Clock);
        Services.AddSingleton(new Random(42));
    }

    protected Progress Progress => Services.GetRequiredService<AppState>().Progress;
    protected Dialogs Dialogs => Services.GetRequiredService<Dialogs>();
    protected NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    private static Question Question(int n, bool isState = false) => new(
        n,
        [new($"Frage {n}")],
        [[new("A")], [new("die Menschen "), new("Steuern", Term: 0), new(" zahlen")], [new("C")], [new("D")]],
        Correct: 3,
        IsState: isState);

    internal sealed class MemoryStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _data = [];
        public string? Read(string key) => _data.GetValueOrDefault(key);
        public void Write(string key, string value) => _data[key] = value;
    }
}
