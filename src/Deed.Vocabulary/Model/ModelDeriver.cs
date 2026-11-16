using System.Globalization;

namespace Deed.Vocabulary.Model;

/// <summary>
/// Builds the model of a validated set of act maps and applies the derivation rules (ADR 0012:
/// the build tool owns them). Rules are numbered as in issue #43.
/// </summary>
public static class ModelDeriver
{
    private const string Deed = Iris.Deed;
    private const string Cli = Iris.Cli;
    private static readonly Term Nothing = Term.Iri(Deed + "Nothing");

    /// <summary>Derives the model. Call only on maps that validate with no violation.</summary>
    /// <exception cref="DerivationException">A rule refuses the maps. Every refusal is reported, not only the first.</exception>
    public static DeedModel Derive(ActMapSet maps)
    {
        ArgumentNullException.ThrowIfNull(maps);
        Deriving d = new(maps);
        DeedModel model = d.Run();
        if (d.Errors.Count > 0)
        {
            throw new DerivationException(d.Errors);
        }

        return model;
    }

    private sealed class Deriving(ActMapSet g)
    {
        public List<DerivationError> Errors { get; } = [];

        public DeedModel Run()
        {
            List<Term> tools = [.. g.OfType(Cli + "Tool")];
            if (tools.Count != 1)
            {
                Errors.Add(new(
                    tools.Count == 0
                        ? "The maps declare no cli:Tool, so no command can be derived. A model is of one tool."
                        : $"The maps declare {tools.Count} tools: {string.Join(", ", tools)}. A model is of one tool.",
                    [.. tools.Select(t => t.Value)]));
                return new DeedModel(new ToolModel("", "", null, null), [], []);
            }

            Term toolTerm = tools[0];
            Term? defaultResource = g.Object(toolTerm, Cli + "defaultResource");
            Term? defaultVerb = g.Object(toolTerm, Cli + "defaultVerb");
            ToolModel tool = new(
                toolTerm.Value,
                g.Text(toolTerm, Cli + "command") ?? "",
                defaultResource is { } r ? Named(r) : null,
                defaultVerb is { } v ? Named(v) : null);

            CheckOperandBoundSettings();

            List<ActModel> acts = [.. g.OfType(Deed + "Act").Select(a => Act(a, tool, defaultResource, defaultVerb))];
            CheckPathsAreUnique(acts);

            List<HostModel> hosts = [.. g.OfType(Cli + "Host").Select(Host)];
            return new DeedModel(tool, acts, hosts);
        }

        // ------------------------------------------------------------- rule 7

        private void CheckOperandBoundSettings()
        {
            List<Term> bound = [.. g.OfType(Deed + "Setting").Where(IsBoundAsOperand)];
            for (int i = 1; i < bound.Count; i++)
            {
                Errors.Add(new(
                    $"{bound[0]} and {bound[i]} are both settings bound as operands. A tool binds at most one setting as an operand (ADR 0018): which one is the target?",
                    [bound[0].Value, bound[i].Value]));
            }

            foreach (Term setting in bound)
            {
                if (g.Text(setting, Cli + "env") is { } env)
                {
                    Errors.Add(new(
                        $"{setting} is bound as an operand and also reads the environment variable {env}. An operand-bound setting has no environment fallback (ADR 0018): an operand is given or the command is refused.",
                        [setting.Value, env]));
                }
            }
        }

        private void CheckPathsAreUnique(List<ActModel> acts)
        {
            foreach (IGrouping<string, ActModel> same in acts
                .Where(a => a.Cli.Command is not null)
                .GroupBy(a => a.Cli.Command!.Line, StringComparer.Ordinal)
                .Where(group => group.Count() > 1))
            {
                List<ActModel> list = [.. same];
                for (int i = 1; i < list.Count; i++)
                {
                    Errors.Add(new(
                        $"{list[0].Iri} and {list[i].Iri} both derive the command '{same.Key}'. One command is one act (ADR 0007, 0010): name a resource or a verb so that each has its own path.",
                        [list[0].Iri, list[i].Iri]));
                }
            }
        }

        // ---------------------------------------------------------------- acts

        private ActModel Act(Term act, ToolModel tool, Term? defaultResource, Term? defaultVerb)
        {
            Term resource = g.Object(act, Deed + "resource") ?? throw Missing(act, "deed:resource");
            Term verb = g.Object(act, Deed + "verb") ?? throw Missing(act, "deed:verb");

            List<Term> chain = [];
            HashSet<Term> seen = [];
            for (Term? r = resource; r is { } t && seen.Add(t); r = g.Object(t, Deed + "within"))
            {
                chain.Insert(0, t);
            }

            List<Term> input = g.Object(act, Deed + "input") is { } head && head != Nothing ? [.. g.List(head)] : [];
            List<Term> contexts = Facet(act, Deed + "context");
            List<Term> changes = [.. g.Objects(act, Deed + "changes").Where(c => c != Nothing)];
            Term outcome = g.Object(act, Deed + "outcome") ?? throw Missing(act, "deed:outcome");

            List<ChangeModel> changeModels = [.. changes
                .Select(c => new ChangeModel(
                    (g.Object(c, Deed + "state") ?? throw Missing(act, "deed:state of a change")).Value,
                    ToManner(g.Object(c, Deed + "manner") ?? throw Missing(act, "deed:manner of a change"))))
                .OrderBy(c => c.State, StringComparer.Ordinal).ThenBy(c => c.Manner)];

            string? spelledAs = g.Text(act, Cli + "spelledAs");
            CliCommand? command = null;
            if (spelledAs is null)
            {
                // Rule 1: resource path outermost first, minus the default resource, then the verb unless default.
                List<string> path = [.. chain.Where(t => t != defaultResource).Select(Token)];
                if (verb != defaultVerb)
                {
                    path.Add(Token(verb));
                }

                command = Command(act, tool.Command, path, input, contexts);
            }

            return new ActModel(
                act.Value,
                Comment(act),
                [.. chain.Select(Named)],
                Named(verb),
                [.. Facet(act, Deed + "actor").Select(a => new ActorModel(a.Value, g.Text(a, Iris.Label), Comment(a)))],
                [.. input.Select(Parameter)],
                [.. contexts.Select(Context)],
                [.. Facet(act, Deed + "needs").Select(s => new StateModel(s.Value, Comment(s)))],
                changeModels,
                new OutcomeModel(
                    g.Is(outcome, Deed + "Stream") ? OutcomeKind.Stream : OutcomeKind.Single,
                    g.Object(outcome, Deed + "yields")?.Value ?? throw Missing(act, "deed:yields")),
                Failures(act, contexts),
                Hints(changeModels),
                new CliModel(command, spelledAs));
        }

        /// <summary>Rule 5 (ADR 0001, 0006).</summary>
        private static HintsModel Hints(List<ChangeModel> changes)
        {
            bool readOnly = changes.Count == 0;
            bool destructive = changes.Any(c => c.Manner is Manner.Converges or Manner.Removes);
            bool idempotent = readOnly || changes.All(c => c.Manner is Manner.Converges or Manner.Removes);
            return new HintsModel(readOnly, destructive, idempotent);
        }

        /// <summary>The act's own failures and its contexts', each once (rule 6 for the exit code).</summary>
        private List<FailureModel> Failures(Term act, List<Term> contexts)
        {
            HashSet<Term> own = [.. Facet(act, Deed + "failure")];
            Dictionary<Term, List<string>> carriers = [];
            foreach (Term context in contexts)
            {
                foreach (Term failure in Facet(context, Deed + "failure"))
                {
                    if (!carriers.TryGetValue(failure, out List<string>? list))
                    {
                        carriers[failure] = list = [];
                    }

                    list.Add(context.Value);
                }
            }

            return [.. own.Union(carriers.Keys)
                .OrderBy(f => f.Value, StringComparer.Ordinal)
                .Select(f => new FailureModel(
                    f.Value,
                    g.Text(f, Deed + "name") ?? "",
                    Comment(f),
                    ExitCode(f),
                    own.Contains(f),
                    carriers.TryGetValue(f, out List<string>? c) ? [.. c.Order(StringComparer.Ordinal)] : []))];
        }

        private int ExitCode(Term failure) =>
            g.Text(failure, Cli + "exitCode") is { } code && int.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 1;

        // ----------------------------------------------------------------- CLI

        private CliCommand Command(Term act, string root, List<string> path, List<Term> input, List<Term> contexts)
        {
            List<CliOperand> lead = [];
            List<CliOperand> subjects = [];
            List<CliOperand> trailing = [];
            List<CliOption> options = [];
            List<CliFlag> flags = [];
            HashSet<string> placed = [];

            void AsOption(Term term, CliSourceKind kind, bool required)
            {
                if (!placed.Add(kind + " " + term.Value))
                {
                    return;
                }

                string datatype = Datatype(term);
                CliSource source = new(kind, term.Value);
                if (datatype == Iris.Boolean)
                {
                    flags.Add(new CliFlag(Token(term), g.Text(term, Cli + "short"), g.Text(term, Cli + "env"), Comment(term), source));
                }
                else
                {
                    options.Add(new CliOption(
                        Token(term), g.Text(term, Cli + "short"), g.Text(term, Cli + "env"), datatype, required, Repeatable(term), Comment(term), source, null));
                }
            }

            // Rule 3, first: the one operand-bound setting. Rule 4: every other setting is an option.
            foreach (Term context in contexts)
            {
                foreach (Term setting in Settings(context))
                {
                    if (IsBoundAsOperand(setting))
                    {
                        if (placed.Add("operand " + setting.Value))
                        {
                            lead.Add(new CliOperand(Token(setting), Datatype(setting), true, Repeatable(setting), Comment(setting), new(CliSourceKind.Setting, setting.Value)));
                        }
                    }
                    else
                    {
                        AsOption(setting, CliSourceKind.Setting, false);
                    }
                }
            }

            // Rule 3: subjects in declared order unless bound as options; modifiers bound as operands after them.
            // Rule 4: every other parameter is an option, and a file alternative adds one more.
            foreach (Term parameter in input)
            {
                bool subject = g.Object(parameter, Deed + "role") == Term.Iri(Deed + "Subject");
                Term? bindAs = g.Object(parameter, Cli + "bindAs");
                bool required = g.Text(parameter, Deed + "required") == "true";
                CliOperand Operand() => new(Token(parameter), Datatype(parameter), required, Repeatable(parameter), Comment(parameter), new(CliSourceKind.Parameter, parameter.Value));

                if (subject && bindAs != Term.Iri(Cli + "Option"))
                {
                    subjects.Add(Operand());
                }
                else if (!subject && bindAs == Term.Iri(Cli + "Operand"))
                {
                    trailing.Add(Operand());
                }
                else
                {
                    AsOption(parameter, CliSourceKind.Parameter, required);
                }

                if (g.Text(parameter, Cli + "fileAlternative") is { } file)
                {
                    options.Add(new CliOption(
                        file, null, null, Deed + "Document", false, false,
                        $"Read {Token(parameter)} from this file instead.",
                        new(CliSourceKind.FileAlternative, parameter.Value),
                        Token(parameter)));
                }
            }

            foreach (Term surface in g.Objects(act, Cli + "surfaceOption").OrderBy(t => t.Value, StringComparer.Ordinal))
            {
                AsOption(surface, CliSourceKind.SurfaceOption, false);
            }

            options.Sort((a, b) => string.CompareOrdinal(a.Long, b.Long));
            flags.Sort((a, b) => string.CompareOrdinal(a.Long, b.Long));
            CheckNamesInCommand(act, options, flags);

            return new CliCommand(
                string.Join(' ', [root, .. path]),
                path,
                [.. lead, .. subjects, .. trailing],
                options,
                flags);
        }

        /// <summary>
        /// Working position, not one of issue #43's rules: two terms that derive the same long or short
        /// name in one command cannot both be given on that command line, so the model is refused.
        /// </summary>
        private void CheckNamesInCommand(Term act, List<CliOption> options, List<CliFlag> flags)
        {
            var named = options.Select(o => (o.Long, o.Short, o.Source.Iri, o.Source.Kind))
                .Concat(flags.Select(f => (f.Long, f.Short, f.Source.Iri, f.Source.Kind)))
                .ToList();

            foreach (var same in named.GroupBy(n => n.Long, StringComparer.Ordinal).Where(x => x.Count() > 1))
            {
                var list = same.ToList();
                for (int i = 1; i < list.Count; i++)
                {
                    Errors.Add(new(
                        $"In the command of {act}, {list[0].Iri} and {list[i].Iri} both derive the option --{same.Key}. Which one does the option mean?",
                        [list[0].Iri, list[i].Iri]));
                }
            }

            foreach (var same in named.Where(n => n.Short is not null).GroupBy(n => n.Short!, StringComparer.Ordinal).Where(x => x.Count() > 1))
            {
                var list = same.ToList();
                for (int i = 1; i < list.Count; i++)
                {
                    Errors.Add(new(
                        $"In the command of {act}, {list[0].Iri} and {list[i].Iri} both have the short name -{same.Key}.",
                        [list[0].Iri, list[i].Iri]));
                }
            }
        }

        // --------------------------------------------------------------- hosts

        private HostModel Host(Term host) => new(
            host.Value,
            g.Text(host, Deed + "name") ?? "",
            Comment(host),
            [.. Facet(host, Deed + "context").Select(Context)],
            [.. g.Objects(host, Cli + "performs").Select(t => t.Value).Order(StringComparer.Ordinal)],
            g.Text(host, Cli + "spelledAs"));

        // --------------------------------------------------------------- terms

        private ContextModel Context(Term context) => new(
            context.Value,
            Comment(context),
            [.. Settings(context).Select(s => new SettingModel(s.Value, g.Text(s, Deed + "name") ?? "", Datatype(s), Repeatable(s), Comment(s)))],
            [.. Facet(context, Deed + "failure").Select(f => f.Value)]);

        private ParameterModel Parameter(Term p) => new(
            p.Value,
            g.Text(p, Deed + "name") ?? "",
            g.Object(p, Deed + "role") == Term.Iri(Deed + "Subject") ? ParameterRole.Subject : ParameterRole.Modifier,
            Datatype(p),
            g.Text(p, Deed + "required") == "true",
            Repeatable(p),
            Comment(p));

        private NamedTerm Named(Term t) => new(t.Value, g.Text(t, Deed + "name") ?? "", g.Text(t, Cli + "name"), Comment(t));

        private List<Term> Settings(Term context) =>
            [.. g.Objects(context, Deed + "setting").Distinct().OrderBy(t => t.Value, StringComparer.Ordinal)];

        /// <summary>A multi-valued facet without deed:Nothing, ordered by IRI: RDF gives it no order.</summary>
        private List<Term> Facet(Term subject, string predicate) =>
            [.. g.Objects(subject, predicate).Where(t => t != Nothing).Distinct().OrderBy(t => t.Value, StringComparer.Ordinal)];

        /// <summary>cli:name where present, else deed:name (rule 1).</summary>
        private string Token(Term t) => g.Text(t, Cli + "name") ?? g.Text(t, Deed + "name") ?? "";

        private string Datatype(Term t) => g.Object(t, Deed + "datatype")?.Value ?? "";

        private bool Repeatable(Term t) => g.Text(t, Deed + "repeatable") == "true";

        private bool IsBoundAsOperand(Term t) => g.Object(t, Cli + "bindAs") == Term.Iri(Cli + "Operand");

        private string? Comment(Term t) => g.Text(t, Iris.Comment);

        private static Manner ToManner(Term manner) => manner.Value switch
        {
            Deed + "Creates" => Manner.Creates,
            Deed + "Appends" => Manner.Appends,
            Deed + "Converges" => Manner.Converges,
            Deed + "Removes" => Manner.Removes,
            _ => throw new InvalidOperationException($"{manner} is not a manner; validation should have refused it."),
        };

        private static InvalidOperationException Missing(Term act, string what) =>
            new($"{act} has no {what}; validation should have refused it.");
    }
}

/// <summary>One refusal by a derivation rule.</summary>
/// <param name="Message">What is refused and why, with full IRIs. Shorten them for printing (ADR 0022).</param>
/// <param name="Terms">The terms the refusal names.</param>
public sealed record DerivationError(string Message, IReadOnlyList<string> Terms);

/// <summary>The maps validate but cannot be derived. No model is written.</summary>
public sealed class DerivationException(IReadOnlyList<DerivationError> errors)
    : Exception(string.Join(Environment.NewLine, errors.Select(e => e.Message)))
{
    /// <summary>Every refusal.</summary>
    public IReadOnlyList<DerivationError> Errors { get; } = errors;
}
