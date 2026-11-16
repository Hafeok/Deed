namespace Deed.Vocabulary.Model;

// The resolved model of a set of act maps, with every derived surface (ADR 0012).
// Plain records: built once from a validated graph, written as JSON by ModelWriter,
// read by nothing in this slice but the tests. docs/model.md describes each field.

/// <summary>The model of one tool: everything the generator needs and nothing it must derive.</summary>
public sealed record DeedModel(ToolModel Tool, IReadOnlyList<ActModel> Acts, IReadOnlyList<HostModel> Hosts)
{
    /// <summary>The format's version. A breaking change raises it.</summary>
    public const int Version = 1;
}

/// <summary>The tool: its root command and its elisions.</summary>
public sealed record ToolModel(string Iri, string Command, NamedTerm? DefaultResource, NamedTerm? DefaultVerb);

/// <summary>A resource or a verb: its act-layer name and, where assigned, its CLI name.</summary>
public sealed record NamedTerm(string Iri, string Name, string? CliName, string? Comment);

/// <summary>One act, its seven facets resolved, and what is derived from them.</summary>
public sealed record ActModel(
    string Iri,
    string? Comment,
    IReadOnlyList<NamedTerm> ResourcePath,
    NamedTerm Verb,
    IReadOnlyList<ActorModel> Actors,
    IReadOnlyList<ParameterModel> Input,
    IReadOnlyList<ContextModel> Contexts,
    IReadOnlyList<StateModel> Needs,
    IReadOnlyList<ChangeModel> Changes,
    OutcomeModel Outcome,
    IReadOnlyList<FailureModel> Failures,
    HintsModel Hints,
    CliModel Cli);

/// <summary>A role that may perform an act.</summary>
public sealed record ActorModel(string Iri, string? Label, string? Comment);

/// <summary>Subject or modifier.</summary>
public enum ParameterRole
{
    /// <summary><c>deed:Subject</c>.</summary>
    Subject,

    /// <summary><c>deed:Modifier</c>.</summary>
    Modifier,
}

/// <summary>A parameter of an act's input, in the act layer.</summary>
public sealed record ParameterModel(
    string Iri, string Name, ParameterRole Role, string Datatype, bool Required, bool Repeatable, string? Comment);

/// <summary>A context of an act or host, with its settings and the IRIs of its failures.</summary>
public sealed record ContextModel(string Iri, string? Comment, IReadOnlyList<SettingModel> Settings, IReadOnlyList<string> Failures);

/// <summary>A setting of a context, in the act layer.</summary>
public sealed record SettingModel(string Iri, string Name, string Datatype, bool Repeatable, string? Comment);

/// <summary>A state an act reads.</summary>
public sealed record StateModel(string Iri, string? Comment);

/// <summary>The manner of a change (ADR 0001).</summary>
public enum Manner
{
    /// <summary><c>deed:Creates</c>.</summary>
    Creates,

    /// <summary><c>deed:Appends</c>.</summary>
    Appends,

    /// <summary><c>deed:Converges</c>.</summary>
    Converges,

    /// <summary><c>deed:Removes</c>.</summary>
    Removes,
}

/// <summary>One write: which state, in what manner.</summary>
public sealed record ChangeModel(string State, Manner Manner);

/// <summary>Single or stream.</summary>
public enum OutcomeKind
{
    /// <summary><c>deed:Single</c>.</summary>
    Single,

    /// <summary><c>deed:Stream</c>.</summary>
    Stream,
}

/// <summary>What an act yields. The kind is an IRI only in this slice (ADR 0013 is later).</summary>
public sealed record OutcomeModel(OutcomeKind Kind, string Yields);

/// <summary>
/// A failure an act can meet: its own, or one of a context it uses. <see cref="Own"/> says whether the act
/// declares it; <see cref="Contexts"/> lists the act's contexts that carry it. One failure can be both.
/// </summary>
public sealed record FailureModel(string Iri, string Name, string? Comment, int ExitCode, bool Own, IReadOnlyList<string> Contexts);

/// <summary>The three hints, derived from the changes (ADR 0001, 0006).</summary>
public sealed record HintsModel(bool ReadOnly, bool Destructive, bool Idempotent);

/// <summary>The act's CLI binding: a derived command, or the recorded spelling that stands in for one.</summary>
public sealed record CliModel(CliCommand? Command, string? SpelledAs);

/// <summary>A derived command.</summary>
/// <param name="Line">The tool's command and the path, joined by spaces: what the user types before any operand.</param>
/// <param name="Path">The tokens after the tool's command.</param>
public sealed record CliCommand(
    string Line,
    IReadOnlyList<string> Path,
    IReadOnlyList<CliOperand> Operands,
    IReadOnlyList<CliOption> Options,
    IReadOnlyList<CliFlag> Flags);

/// <summary>Where a CLI element comes from.</summary>
public enum CliSourceKind
{
    /// <summary>A parameter of the act's input.</summary>
    Parameter,

    /// <summary>A setting of one of the act's contexts.</summary>
    Setting,

    /// <summary>A <c>cli:SurfaceOption</c> of the act.</summary>
    SurfaceOption,

    /// <summary>The <c>cli:fileAlternative</c> of a parameter: the parameter's value read from a file.</summary>
    FileAlternative,
}

/// <summary>The term a CLI element is bound to.</summary>
public sealed record CliSource(CliSourceKind Kind, string Iri);

/// <summary>An operand, in position order.</summary>
public sealed record CliOperand(string Name, string Datatype, bool Required, bool Repeatable, string? Comment, CliSource Source);

/// <summary>An option that takes a value.</summary>
/// <param name="AlternativeTo">For a file alternative, the long name of the option it stands in for. Exactly one of the two is given.</param>
public sealed record CliOption(
    string Long, string? Short, string? Env, string Datatype, bool Required, bool Repeatable, string? Comment, CliSource Source, string? AlternativeTo);

/// <summary>An option that takes no value.</summary>
public sealed record CliFlag(string Long, string? Short, string? Env, string? Comment, CliSource Source);

/// <summary>A host command (ADR 0003).</summary>
public sealed record HostModel(
    string Iri, string Name, string? Comment, IReadOnlyList<ContextModel> Contexts, IReadOnlyList<string> Performs, string? SpelledAs);
