using System.Text;
using System.Text.Json;
using Json.Schema;
using Deed.Vocabulary;
using Deed.Vocabulary.Model;

namespace Deed.Tests;

/// <summary>
/// The model against the reference tables in <c>tests/fixtures/*.commands.tsv</c>, which
/// <c>tests/reference/commands.py</c> derived from the same rules. A row that differs is a rule read
/// two ways: it is a finding, listed in <see cref="KnownDifferences"/> with its cause, not made to pass.
/// </summary>
public sealed class ModelTests
{
    public static TheoryData<string, string> Maps => new()
    {
        { "maps/varve/varve-acts.ttl", "varve" },
        { "maps/product-cli/product-acts.ttl", "product-cli" },
    };

    private static readonly string[] Columns =
        ["act", "command", "operands", "options", "flags", "read-only", "destructive", "idempotent", "exit-codes", "note"];

    /// <summary>
    /// Every cell where the model and a reference table differ: fixture, act, column, table value, model value.
    /// Cause, for all of them: issue #43 rule 2 gives an act with <c>cli:spelledAs</c> no CLI command, so the
    /// model has no options or flags for it; <c>commands.py</c> blanks the command and the operands of such an
    /// act and still lists the options and flags its contexts, input and surface options would derive.
    /// </summary>
    private static readonly string[] KnownDifferences =
    [
        "product-cli ListFormats: none",
        "varve ApplyDeclarations options: '--assertion-audience --authority --client-assertion --client-id --client-secret(VARVE_CLIENT_SECRET) --out --scope --server --token(VARVE_TOKEN)' in the table, '' in the model",
        "varve ApplyPlan options: '--assertion-audience --authority --client-assertion --client-id --client-secret(VARVE_CLIENT_SECRET) --scope --server --token(VARVE_TOKEN)' in the table, '' in the model",
        "varve BreakLease options: '--endpoint --region --sts-endpoint' in the table, '' in the model",
        "varve MoveEventDeclarations options: '--assertion-audience --authority --client-assertion --client-id --client-secret(VARVE_CLIENT_SECRET) --scope --server --token(VARVE_TOKEN)' in the table, '' in the model",
        "varve PlanDeclarations flags: '--exit-code' in the table, '' in the model",
        "varve PlanDeclarations options: '--assertion-audience --authority --client-assertion --client-id --client-secret(VARVE_CLIENT_SECRET) --out --scope --server --token(VARVE_TOKEN)' in the table, '' in the model",
        "varve ShowConfiguration flags: '--anonymous --record-bootstrap' in the table, '' in the model",
        "varve ShowConfiguration options: '--audience* --auth-mode --authority --config* --datasets-root --set* --urls' in the table, '' in the model",
        "varve TakeOverLease options: '--endpoint --epoch --region --sts-endpoint' in the table, '' in the model",
        "varve TakeOverWriter flags: '--managed-identity' in the table, '' in the model",
        "varve TakeOverWriter options: '--assertion-audience --authority --client-assertion --client-id --client-secret(VARVE_CLIENT_SECRET) --from --scope --server* --token(VARVE_TOKEN)' in the table, '' in the model",
    ];

    /// <summary>Loaded once: the library registers a schema by its $id and refuses to register it twice.</summary>
    private static readonly Lazy<JsonSchema> Schema = new(() => JsonSchema.FromText(File.ReadAllText(Repository.File("docs/model.schema.json"))));

    private static DeedModel Derive(string map) => ModelDeriver.Derive(ActMapSet.ReadFiles([Repository.File(map)]));

    [Fact]
    public void Rows_match_the_reference_tables_except_the_known_differences()
    {
        List<string> differences = [];
        foreach ((string map, string fixture) in new[] { ("maps/varve/varve-acts.ttl", "varve"), ("maps/product-cli/product-acts.ttl", "product-cli") })
        {
            DeedModel model = Derive(map);
            string[] lines = File.ReadAllLines(Repository.File($"tests/fixtures/{fixture}.commands.tsv"));
            Assert.Equal(string.Join('\t', Columns), lines[0]);

            Dictionary<string, string[]> table = lines.Skip(1).Where(l => l.Length > 0).Select(l => l.Split('\t')).ToDictionary(f => f[0]);
            Assert.Equal(table.Keys.Order(StringComparer.Ordinal), model.Acts.Select(a => a.Iri).Order(StringComparer.Ordinal));

            foreach (ActModel act in model.Acts)
            {
                string[] expected = table[act.Iri];
                string[] actual = Row(act);
                for (int c = 1; c < Columns.Length; c++)
                {
                    if (expected[c] != actual[c])
                    {
                        differences.Add($"{fixture} {Local(act.Iri)} {Columns[c]}: '{expected[c]}' in the table, '{actual[c]}' in the model");
                    }
                }
            }
        }

        differences.Sort(StringComparer.Ordinal);
        Assert.Equal(KnownDifferences.Where(d => !d.EndsWith(": none", StringComparison.Ordinal)), differences);
    }

    [Theory]
    [InlineData("maps/varve/varve-acts.ttl", 22, 13, 2)]
    [InlineData("maps/product-cli/product-acts.ttl", 32, 31, 3)]
    public void Counts_of_acts_commands_and_hosts(string map, int acts, int commands, int hosts)
    {
        DeedModel model = Derive(map);

        Assert.Equal(acts, model.Acts.Count);
        Assert.Equal(commands, model.Acts.Count(a => a.Cli.Command is not null));
        Assert.Equal(acts - commands, model.Acts.Count(a => a.Cli.Command is null && a.Cli.SpelledAs is not null));
        Assert.Equal(hosts, model.Hosts.Count);
    }

    [Theory]
    [MemberData(nameof(Maps))]
    public void The_golden_model_is_what_the_map_derives_byte_for_byte(string map, string fixture)
    {
        byte[] first = ModelWriter.ToUtf8(Derive(map));
        byte[] second = ModelWriter.ToUtf8(Derive(map));

        Assert.Equal(first, second);
        Assert.Equal(File.ReadAllBytes(Repository.File($"tests/fixtures/{fixture}.model.json")), first);
    }

    [Theory]
    [MemberData(nameof(Maps))]
    public void The_golden_model_validates_against_the_json_schema(string map, string fixture)
    {
        _ = map;
        JsonSchema schema = Schema.Value;
        using JsonDocument golden = JsonDocument.Parse(File.ReadAllBytes(Repository.File($"tests/fixtures/{fixture}.model.json")));

        EvaluationResults results = schema.Evaluate(golden.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(results.IsValid, string.Join(Environment.NewLine, (results.Details ?? []).Where(d => d.Errors is not null)
            .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"))));
    }

    [Fact]
    public void The_json_schema_refuses_a_model_that_is_not_one()
    {
        JsonSchema schema = Schema.Value;
        string golden = File.ReadAllText(Repository.File("tests/fixtures/varve.model.json"));

        foreach (string broken in new[]
        {
            golden.Replace("\"deedModel\": 1", "\"deedModel\": 2", StringComparison.Ordinal),
            golden.Replace("\"manner\": \"converges\"", "\"manner\": \"replaces\"", StringComparison.Ordinal),
            golden.Replace("\"readOnly\":", "\"readonly\":", StringComparison.Ordinal),
        })
        {
            Assert.NotEqual(golden, broken);
            using JsonDocument document = JsonDocument.Parse(broken);
            Assert.False(schema.Evaluate(document.RootElement).IsValid);
        }
    }

    [Fact]
    public void An_act_without_a_command_keeps_its_facets_hints_and_the_spelling_as_reason()
    {
        ActModel act = Assert.Single(Derive("maps/varve/varve-acts.ttl").Acts, a => a.Iri.EndsWith("#BreakLease", StringComparison.Ordinal));

        Assert.Null(act.Cli.Command);
        Assert.Equal("lease <dataset> --break", act.Cli.SpelledAs);
        Assert.Equal(new HintsModel(false, true, true), act.Hints);
        Assert.Equal(["lock-held", "no-lock-here", "not-a-dataset"], act.Failures.Where(f => f.Name is "lock-held" or "no-lock-here" or "not-a-dataset").Select(f => f.Name));
    }

    // ----------------------------------------------------------- the table's notation

    /// <summary>One row in the notation of <c>commands.tsv</c>.</summary>
    private static string[] Row(ActModel act)
    {
        CliCommand? c = act.Cli.Command;
        string Bool(bool b) => b ? "true" : "false";
        return
        [
            act.Iri,
            c?.Line ?? "",
            c is null ? "" : string.Join(' ', c.Operands.Select(Operand)),
            c is null ? "" : string.Join(' ', c.Options.Select(o => Option(o.Long, o.Short, o.Env, o.Repeatable)).Distinct().Order(StringComparer.Ordinal)),
            c is null ? "" : string.Join(' ', c.Flags.Select(f => Option(f.Long, f.Short, f.Env, false)).Distinct().Order(StringComparer.Ordinal)),
            Bool(act.Hints.ReadOnly),
            Bool(act.Hints.Destructive),
            Bool(act.Hints.Idempotent),
            string.Join(' ', act.Failures.Where(f => f.ExitCode != 1).Select(f => $"{f.Name}={f.ExitCode}").Distinct().Order(StringComparer.Ordinal)),
            act.Cli.SpelledAs is { } s ? "recorded spelling: " + s : "",
        ];
    }

    private static string Operand(CliOperand o)
    {
        // The table marks repetition and optionality on parameters only; an operand-bound setting is always required.
        if (o.Source.Kind == CliSourceKind.Setting)
        {
            return o.Name;
        }

        string s = o.Name + (o.Repeatable ? "..." : "");
        return o.Required ? s : "[" + s + "]";
    }

    private static string Option(string name, string? shortName, string? env, bool repeatable)
    {
        StringBuilder s = new("--" + name);
        if (shortName is not null) s.Append("|-").Append(shortName);
        if (env is not null) s.Append('(').Append(env).Append(')');
        if (repeatable) s.Append('*');
        return s.ToString();
    }

    private static string Local(string iri) => iri[(iri.LastIndexOf('#') + 1)..];
}
