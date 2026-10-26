using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Scope = "namespace", Target = "~N:Ebt.App.Shared",
    Justification = "\"Shared\" is a keyword only in Visual Basic; this app's types are not consumed from other languages.")]
