namespace Ebt.Pipeline.Tests;

public class ValidationTests
{
    [Fact]
    public void Valid_sources_have_no_problems() => Assert.Empty(Validation.Check(Fixtures.Valid(), Fixtures.Shape));

    [Fact]
    public void Reports_a_question_without_text() =>
        AssertSingleProblem(Fixtures.Valid(texts: new Dictionary<int, string>()), "1: no question text");

    [Fact]
    public void Reports_a_correct_index_out_of_range() =>
        AssertSingleProblem(
            Fixtures.Valid(answers: [new BamfAnswer(1, 100, 4, ["Monarchie", "Republik", "Diktatur", "1949"])]),
            "1: correct answer index 4 out of range");

    [Fact]
    public void Reports_a_missing_translation() =>
        AssertSingleProblem(Fixtures.Valid(translations: new Dictionary<int, Translation>()), "1: translation missing");

    [Fact]
    public void Reports_a_changed_number_answer() =>
        AssertSingleProblem(
            Fixtures.Valid(translations: Fixtures.WithAnswers("монархия", "республика", "диктатура", "1949 г.")),
            "1: answer 4 \"1949\" must stay unchanged, got \"1949 г.\"");

    [Fact]
    public void Reports_lost_emphasis()
    {
        var src = Fixtures.Valid(texts: new Dictionary<int, string> { [1] = "Was ist _keine_ Staatsform?" });
        AssertSingleProblem(src, "1: emphasis (_…_) lost in translation");
    }

    [Fact]
    public void Reports_a_gloss_that_only_the_correct_answer_has() =>
        AssertSingleProblem(
            Fixtures.Valid(translations: Fixtures.WithAnswers("монархия", "республика (выборный глава)", "диктатура", "1949")),
            "1: only the correct answer has a bracketed gloss – it gives the answer away");

    [Fact]
    public void Accepts_glosses_on_wrong_answers_too() =>
        Assert.Empty(Validation.Check(Fixtures.Valid(
            translations: Fixtures.WithAnswers("монархия (власть короля)", "республика (выборный глава)", "диктатура", "1949")),
            Fixtures.Shape));

    [Fact]
    public void Reports_missing_questions_as_ranges() =>
        AssertSingleProblem(Fixtures.Valid(), new CatalogShape(General: 5, State: 2), "questions missing: 2–5, 301–302");

    [Fact]
    public void Reports_a_question_that_appears_twice()
    {
        var answer = Fixtures.Valid().Answers[0];
        AssertSingleProblem(Fixtures.Valid(answers: [answer, answer]), "1: question appears 2 times");
    }

    [Fact]
    public void Reports_a_number_outside_the_catalogue()
    {
        var src = Fixtures.Valid() with
        {
            Answers = [.. Fixtures.Valid().Answers, new BamfAnswer(311, 101, 0, ["a", "b", "c", "d"])],
            Texts = new Dictionary<int, string> { [1] = Fixtures.Valid().Texts[1], [311] = "Eine Frage?" },
            Translations = new Dictionary<int, Translation>
            {
                [1] = Fixtures.Valid().Translations[1],
                [311] = new("Вопрос?", ["а", "б", "в", "г"]),
            },
        };
        AssertSingleProblem(src, "311: not a question of this catalogue (expected 1)");
    }

    [Fact]
    public void Accepts_general_and_state_questions_in_their_ranges()
    {
        var src = Fixtures.Valid() with
        {
            Answers = [.. Fixtures.Valid().Answers, new BamfAnswer(301, 101, 0, ["a", "b", "c", "d"])],
            Texts = new Dictionary<int, string> { [1] = Fixtures.Valid().Texts[1], [301] = "Eine Frage?" },
            Translations = new Dictionary<int, Translation>
            {
                [1] = Fixtures.Valid().Translations[1],
                [301] = new("Вопрос?", ["а", "б", "в", "г"]),
            },
        };
        Assert.Empty(Validation.Check(src, new CatalogShape(General: 1, State: 1)));
    }

    [Fact]
    public void Reports_a_word_form_used_by_two_terms() =>
        AssertSingleProblem(
            Fixtures.Valid(glossary:
            [
                new("die Republik", ["Republik"], "республика", "…"),
                new("die Bundesrepublik", ["Republik"], "федеративная республика", "…"),
            ]),
            "glossary: form \"Republik\" in both \"die Republik\" and \"die Bundesrepublik\"");

    [Fact]
    public void Reports_a_term_that_occurs_in_no_question() =>
        AssertSingleProblem(
            Fixtures.Valid(glossary:
            [
                new("die Republik", ["Republik"], "республика", "…"),
                new("die Monarchie", ["Monarchien"], "монархии", "…"),
            ]),
            "glossary \"die Monarchie\": occurs in no question");

    [Fact]
    public void Reports_a_picture_that_belongs_to_no_question() =>
        AssertSingleProblem(Fixtures.Valid(pictures: new HashSet<int> { 999 }), "pictures/999.png belongs to no question");

    private static void AssertSingleProblem(SourceData src, string expected) =>
        AssertSingleProblem(src, Fixtures.Shape, expected);

    private static void AssertSingleProblem(SourceData src, CatalogShape shape, string expected) =>
        Assert.Equal([expected], Validation.Check(src, shape));
}
