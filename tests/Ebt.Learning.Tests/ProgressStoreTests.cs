using System.Text.Json.Nodes;

namespace Ebt.Learning.Tests;

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
        progress.StartPractice(PracticeMode.Random, Fixtures.Catalog(5, 0).Questions, Fixtures.Random());
        progress.Practice.Next();
        progress.Practice.Next();
        var catalog = Fixtures.Catalog();
        progress.StartExam(catalog.Questions, Fixtures.Now, Fixtures.Random()).Answer(3, Fixtures.Now);
        var result = progress.FinishExam(catalog, Fixtures.Now);
        var exam = progress.StartExam(catalog.Questions, Fixtures.Now, Fixtures.Random());
        exam.Answer(3, Fixtures.Now);
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

    /// <summary>
    /// Schema 2 exactly as the released app stores it on the phone. Never edit this JSON: it fails when a renamed
    /// or retyped property would silently load the user's progress as empty. Changing the stored shape means a new
    /// schema number, a migration in <see cref="ProgressStore"/> and a new test like this one for the new schema.
    /// </summary>
    private const string StoredSchema2 = """
        {"schema":2,"progress":{
          "box":{"1":2,"2":0,"3":0},
          "wrong":{"2":1,"3":1},
          "practice":{"mode":"Random","order":[3,2,1,301],"position":1},
          "exam":{"started":"2026-10-13T11:00:00+00:00","questions":[1,3,2,301],"answers":[2,null,null,null],"position":0},
          "exams":[{"date":"2026-10-13T10:20:00+00:00","questions":[3,1,2,301],"answers":[1,null,null,null],"wrong":[3,1,2,301]}]
        }}
        """;

    [Fact]
    public void Progress_stored_by_the_released_app_loads_completely()
    {
        var loaded = new ProgressStore(new MemoryStore(new() { ["ebt.progress"] = StoredSchema2 })).Load();

        Assert.Equal(new Dictionary<int, int> { [1] = 2, [2] = 0, [3] = 0 }, loaded.Box);
        Assert.Equal(new Dictionary<int, int> { [2] = 1, [3] = 1 }, loaded.Wrong);

        Assert.Equal((PracticeMode.Random, 1), (loaded.Practice.Mode, loaded.Practice.Position));
        Assert.Equal([3, 2, 1, 301], loaded.Practice.Order);

        var exam = Assert.IsType<Exam>(loaded.Exam);
        Assert.Equal(Fixtures.Now.AddHours(1), exam.Started);
        Assert.Equal([1, 3, 2, 301], exam.Questions);
        Assert.Equal([2, null, null, null], exam.Answers);
        Assert.Equal(0, exam.Position);

        var result = Assert.Single(loaded.Exams);
        Assert.Equal(Fixtures.Now.AddMinutes(20), result.Date);
        Assert.Equal([3, 1, 2, 301], result.Questions);
        Assert.Equal([1, null, null, null], result.Answers);
        Assert.Equal([3, 1, 2, 301], result.Wrong);
    }

    [Fact]
    public void Progress_is_saved_in_the_shape_the_released_app_stored()
    {
        var memory = new MemoryStore(new() { ["ebt.progress"] = StoredSchema2 });
        var store = new ProgressStore(memory);

        store.Save(store.Load());

        var saved = memory.Data["ebt.progress"];
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(StoredSchema2), JsonNode.Parse(saved)), saved);
    }

    [Fact]
    public void Progress_stored_with_another_schema_is_not_misread()
    {
        // The unreleased JavaScript prototype stored schema 1 in a different shape.
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
