using System.Text;
using System.Text.Json;
using Json.Schema;
using Deed.Vocabulary;
using Deed.Vocabulary.Model;

namespace Deed.Tests;

/// <summary>
/// The model against the reference tables in <c>tests/fixtures/*.commands.tsv</c>, which
/// <c>tests/reference/commands.py</c> derives from the same rules. A row that differs is a rule read
/// two ways: a finding to report, never something to make pass by editing the table or the test.
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

    /// <summary>Loaded once: the library registers a schema by its $id and refuses to register it twice.</summary>
    private static readonly Lazy<JsonSchema> Schema = new(() => JsonSchema.FromText(File.ReadAllText(Repository.File("docs/model.schema.json"))));

    private static DeedModel Derive(string map) => ModelDeriver.Derive(ActMapSet.ReadFiles([Repository.File(map)]));

    [Theory]
    [MemberData(nameof(Maps))]
    public void Rows_match_the_reference_table(string map, string fixture)
    {
        string[] lines = File.ReadAllLines(Repository.File($"tests/fixtures/{fixture}.commands.tsv"));
        Assert.Equal(string.Join('\t', Columns), lines[0]);

        List<string> expected = [.. lines.Skip(1).Where(l => l.Length > 0)];
        List<string> actual = [.. Derive(map).Acts.Select(a => string.Join('\t', Row(a)))];

        // Acts are in IRI order in both. One row per line, so a failure names the act that differs.
        Assert.Equal(expected, actual);
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

    /// <summary>
    /// One row in the notation of <c>commands.tsv</c>. An act without a command has nothing of a command
    /// line: no operands, options, flags or exit codes. Its hints and its recorded spelling remain.
    /// </summary>
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
            c is null ? "" : string.Join(' ', act.Failures.Where(f => f.ExitCode != 1).Select(f => $"{f.Name}={f.ExitCode}").Distinct().Order(StringComparer.Ordinal)),
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
}
