using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Ebt.App;
using Ebt.Content;
using Ebt.Learning;
using Microsoft.AspNetCore.Components;
using Mono.Cecil;
using NetArchTest.Rules;

namespace Ebt.Architecture.Tests;

/// <summary>
/// The structure the code must keep, checked on the compiled code: which part may use which. A rule written only in a
/// README erodes one convenient call at a time. Each rule says why it exists. Some rules are kept by the compiler
/// instead: the project references (the pipeline references Ebt.Content only, ADR 9) and the internal constructors
/// that only ProgressStore uses to rebuild stored progress (ADR 10).
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Content = typeof(Catalog).Assembly;
    private static readonly Assembly Learning = typeof(Progress).Assembly;
    private static readonly Assembly App = typeof(ServiceRegistration).Assembly;

    private const string Features = "Ebt.App.Features";

    /// <summary>One module per feature; the rest of the app is the shell (layout, routing, registration), Shared and Infrastructure.</summary>
    private static readonly string[] FeatureNames = ["Home", "Practice", "Review", "MockExam", "Glossary", "Stats"];

    public static TheoryData<string> Feature => new(FeatureNames);

    [Fact]
    public void The_domain_is_plain_code_without_ui_network_or_files() => Holds(
        Types.InAssemblies([Content, Learning]).ShouldNot().HaveDependencyOnAny(
            "Microsoft.AspNetCore", "Microsoft.JSInterop", "System.Net", "System.IO", "Ebt.App", "Ebt.Pipeline"),
        "Ebt.Content runs in the browser and in the pipeline, Ebt.Learning in the browser, and their tests need no "
        + "fakes: they compute, and leave I/O to the app and the pipeline (README, Architecture).");

    [Fact]
    public void The_learning_model_knows_no_json() => Holds(
        Types.InAssembly(Learning).That().DoNotHaveNameMatching(@"^(ProgressStore|StoredProgress|ProgressJson|ProgressSchema2|\w+V2)$")
            .ShouldNot().HaveDependencyOnAny("System.Text.Json"),
        "Progress is stored in records of its own (ProgressSchema2.cs), mapped in ProgressStore: the model can then "
        + "change freely, and the stored format changes only when a schema does (ADR 10).");

    [Fact]
    public void The_domain_s_state_changes_only_through_its_methods()
    {
        // Records may have init: they are values, set once.
        var settable = Content.GetExportedTypes().Concat(Learning.GetExportedTypes())
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.SetMethod is { IsPublic: true } set
                    && !set.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)))
                .Select(p => $"{type.FullName}.{p.Name}"));

        Assert.True(!settable.Any(),
            "Progress, Exam and PracticeRun keep their rules (Leitner boxes, the 60 minutes) only if state changes "
            + $"through their methods (README, Engineering highlights). Public setters: {string.Join(", ", settable)}");
    }

    [Fact]
    public void Every_type_of_the_app_is_in_one_of_its_modules() => Holds(
        Types.InAssembly(App).That().ResideInNamespaceMatching(Within("Ebt.App"))
            .Should().ResideInNamespaceMatching(
                $@"^Ebt\.App(\.Shared|\.Infrastructure|\.Features\.({string.Join('|', FeatureNames)}))?$"),
        "The app is split by feature (FoSA, modular monolith): a new folder is either a new feature, added to "
        + "FeatureNames here, or belongs into an existing module.");

    [Theory]
    [MemberData(nameof(Feature))]
    public void A_feature_uses_no_other_feature(string feature) => Holds(
        Types.InAssembly(App).That().ResideInNamespaceMatching(Within($"{Features}.{feature}"))
            .ShouldNot().HaveDependencyOnAny([.. FeatureNames.Where(f => f != feature).Select(f => $"{Features}.{f}")]),
        "Features are independent modules: a change to one doesn't reach another. They share code through Shared, "
        + "and lead to each other by address (Nav.NavigateTo), not by type.");

    [Fact]
    public void Shared_and_infrastructure_know_no_feature() => Holds(
        Types.InAssembly(App).That().ResideInNamespaceMatching(Within("Ebt.App.Shared")).Or()
            .ResideInNamespaceMatching(Within("Ebt.App.Infrastructure"))
            .ShouldNot().HaveDependencyOnAny(Features),
        "Dependencies point from the features down to Shared and Infrastructure, never back: what they hold must work "
        + "for every feature.");

    [Fact]
    public void Only_infrastructure_uses_javascript() => Holds(
        Types.InAssembly(App).That().DoNotResideInNamespaceMatching(Within("Ebt.App.Infrastructure"))
            .ShouldNot().HaveDependencyOnAny("Microsoft.JSInterop"),
        "The browser is reached through Infrastructure (LocalStorage, BrowserWindow), which tests replace or run in "
        + "loose mode; pages and services stay plain .NET.");

    /// <summary>
    /// The methods of the learning state that only answer a question. Every other method changes the state: a new one
    /// counts as a change until it is listed here.
    /// </summary>
    private static readonly Dictionary<string, string[]> Queries = new()
    {
        ["Ebt.Learning.Progress"] = ["BoxOf", "IsNew", "CountMastered", "CountMistakes", "CountBoxes"],
        ["Ebt.Learning.Exam"] = ["Remaining", "IsOver", "IsAbandoned"],
    };

    [Fact]
    public void Components_ask_the_learning_state_questions_but_change_nothing_themselves()
    {
        // Pages read AppState.Progress. A page that changed it directly would show a change that is never saved:
        // changes go through the session services, which save each one (AppState.Change, ADR 11). Read from the
        // compiled code: dependency rules can't tell a question from a change, both are calls on the same type.
        var components = App.GetTypes().Where(t => t.IsAssignableTo(typeof(ComponentBase))).Select(t => t.FullName).ToHashSet();
        using var app = ModuleDefinition.ReadModule(App.Location);
        var changes = Nested(app.Types).Where(type => components.Contains(Outermost(type).FullName))
            .SelectMany(type => type.Methods.Where(m => m.HasBody)
                .SelectMany(m => m.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>())
                .Where(called => IsChange(called))
                .Select(called => $"{Outermost(type).FullName} calls {called.DeclaringType.Name}.{called.Name}"))
            .Distinct().ToList();

        Assert.True(changes.Count == 0,
            "Pages change the learning state only through the session services of their feature, which save every "
            + "change (AppState.Change). If the method only answers a question, add it to Queries.\n" + string.Join("\n", changes));
    }

    private static bool IsChange(MethodReference called)
    {
        var type = called.DeclaringType.FullName;
        if (type == "Ebt.App.Shared.AppState") return called.Name == "Change";
        return type.StartsWith("Ebt.Learning.", StringComparison.Ordinal)
            && !called.Name.StartsWith("get_", StringComparison.Ordinal)
            && !(Queries.TryGetValue(type, out var queries) && queries.Contains(called.Name));
    }

    // The namespace and the ones below it.
    private static string Within(string ns) => $@"^{Regex.Escape(ns)}(\..+)?$";

    // Lambdas and async methods compile to nested types, which have no namespace of their own.
    private static IEnumerable<TypeDefinition> Nested(IEnumerable<TypeDefinition> types) =>
        types.SelectMany(type => Nested(type.NestedTypes).Prepend(type));

    private static TypeDefinition Outermost(TypeDefinition type) => type.DeclaringType is { } outer ? Outermost(outer) : type;

    private static void Holds(ConditionList rule, string why)
    {
        var result = rule.GetResult();
        Assert.True(result.IsSuccessful, $"{why}\nBroken by: {string.Join(", ", Breaking(result))}");
    }

    // The types that break a rule. Lambdas and async methods compile to nested types: they count as the type they're in.
    private static string[] Breaking(TestResult result) =>
        [.. (result.FailingTypes ?? []).Select(t => t.FullName.Split('/', '+')[0]).Distinct().Order(StringComparer.Ordinal)];
}
