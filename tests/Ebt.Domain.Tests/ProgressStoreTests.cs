namespace Ebt.Domain.Tests;

public class ProgressStoreTests
{
    /// <summary>In-memory stand-in for localStorage.</summary>
    private sealed class MemoryStore(Dictionary<string, string>? initial = null) : IKeyValueStore
    {
        public Dictionary<string, string> Data { get; } = initial ?? [];
        public string? Read(string key) => Data.GetValueOrDefault(key);
        public void Write(string key, string value) => Data[key] = value;
    }

    [Fact]
    public void Starts_with_empty_progress()
    {
        var progress = new ProgressStore(new MemoryStore()).Load();

        Assert.Empty(progress.Box);
        Assert.Null(progress.Exam);
    }

    [Fact]
    public void Saved_progress_loads_back_unchanged()
    {
        var memory = new MemoryStore();
        var progress = new Progress();
        progress.Record(12, false);
        progress.Record(12, true);
        progress.StartPractice(PracticeMode.Random, Fixtures.Catalog(5, 0).Questions, Fixtures.Random(), position: 2);
        var catalog = Fixtures.Catalog();
        progress.StartExam(catalog.Questions, Fixtures.Now, Fixtures.Random()).Answer(3);
        var result = progress.FinishExam(catalog, Fixtures.Now);
        var exam = progress.StartExam(catalog.Questions, Fixtures.Now, Fixtures.Random());
        exam.Answer(3);
        new ProgressStore(memory).Save(progress);

        var loaded = new ProgressStore(memory).Load();

        Assert.Equal(progress.Box, loaded.Box);
        Assert.Equal(progress.Wrong, loaded.Wrong);
        Assert.Equal((PracticeMode.Random, 2), (loaded.Practice.Mode, loaded.Practice.Position));
        Assert.Equal(progress.Practice.Order, loaded.Practice.Order);
        Assert.Equal(exam.Started, loaded.Exam?.Started);
        Assert.Equal(exam.Questions, loaded.Exam?.Questions);
        Assert.Equal(exam.Answers, loaded.Exam?.Answers);
        var loadedResult = Assert.Single(loaded.Exams);
        Assert.Equal(result.Date, loadedResult.Date);
        Assert.Equal(result.Answers, loadedResult.Answers);
        Assert.Equal(result.Wrong, loadedResult.Wrong);
    }

    [Fact]
    public void Progress_stored_with_another_schema_is_not_misread()
    {
        // The JavaScript version stored schema 1 in a different shape.
        var memory = new MemoryStore(new() { ["ebt.progress"] = """{"schema":1,"box":{"1":3},"wrong":{}}""" });

        Assert.Empty(new ProgressStore(memory).Load().Box);
    }

    [Fact]
    public void Unreadable_stored_data_does_not_crash_the_app()
    {
        var memory = new MemoryStore(new() { ["ebt.progress"] = "{not json" });

        Assert.Empty(new ProgressStore(memory).Load().Box);
    }
}
