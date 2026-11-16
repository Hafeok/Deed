using System.Diagnostics;

namespace Deed.Tests;

/// <summary>Exit codes and output, by running the built tool.</summary>
public sealed class ToolTests
{
    private sealed record Run(int ExitCode, string Output, string Error);

    private static Run Deed(params string[] args)
    {
        string tool = Path.Combine(AppContext.BaseDirectory, "Deed.Tool.dll");
        Assert.True(File.Exists(tool), $"The built tool is not at {tool}.");

        ProcessStartInfo start = new("dotnet")
        {
            WorkingDirectory = Repository.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(tool);
        foreach (string arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        process.WaitForExit();
        return new Run(process.ExitCode, output.Result, error.Result);
    }

    [Fact]
    public void No_violation_exits_0_even_with_warnings_and_infos()
    {
        Run run = Deed("validate", "maps/varve/varve-acts.ttl");

        Assert.Equal(0, run.ExitCode);
        Assert.EndsWith("0 violations, 10 warnings, 8 infos." + Environment.NewLine, run.Output, StringComparison.Ordinal);
        Assert.Empty(run.Error);
    }

    [Fact]
    public void Product_cli_exits_0()
    {
        Run run = Deed("validate", "maps/product-cli/product-acts.ttl");

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("0 violations, 2 warnings, 4 infos.", run.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_violation_exits_1()
    {
        Run run = Deed("validate", "tests/fixtures/broken.ttl");

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("22 violations, 0 warnings, 0 infos.", run.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Results_are_grouped_by_focus_node_with_severity_and_full_message_iris_shortened()
    {
        Run run = Deed("validate", "tests/fixtures/broken.ttl");

        // The entry for x:force: its heading once, then its results until a blank line. IRIs under a
        // prefix the map declares are printed short (ADR 0022); the shapes' namespace is not declared there.
        string[] lines = run.Output.Split(Environment.NewLine);
        Assert.Equal(1, lines.Count(l => l == "x:force"));
        string[] entry = [.. lines.SkipWhile(l => l != "x:force").Skip(1).TakeWhile(l => l.Length > 0)];

        string[] expected =
        [
            "  Violation: 'force' is also the name of x:path. A name means one thing across the tool. Are these the same parameter, or does one of them need a more exact name?",
            "    (shape https://w3id.org/deed/shapes#NameMeansOneThing)",
            "  Violation: A required yes-or-no has only one useful value. What is the actor confirming? If it is that they have seen the current state, ask for that state instead.",
            "    (shape https://w3id.org/deed/shapes#FlagIsNotRequired)",
        ];
        Assert.Equal(expected, entry);
    }

    [Fact]
    public void Several_maps_are_one_data_graph()
    {
        Run run = Deed("validate", "maps/varve/varve-acts.ttl", "tests/fixtures/broken.ttl");

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("violations, 10 warnings, 8 infos.", run.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData]
    [InlineData("validate")]
    [InlineData("check", "maps/varve/varve-acts.ttl")]
    [InlineData("validate", "--strict", "maps/varve/varve-acts.ttl")]
    public void A_usage_error_exits_2(params string[] args)
    {
        Run run = Deed(args);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("Usage: deed validate <map.ttl>...", run.Error, StringComparison.Ordinal);
        Assert.Empty(run.Output);
    }

    [Fact]
    public void A_missing_file_exits_2()
    {
        Run run = Deed("validate", "maps/no-such-map.ttl");

        Assert.Equal(2, run.ExitCode);
        Assert.StartsWith("deed: maps/no-such-map.ttl: cannot be read", run.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_that_does_not_parse_exits_2_with_the_parsers_position()
    {
        string path = Path.Combine(Path.GetTempPath(), $"deed-{Guid.NewGuid():N}.ttl");
        File.WriteAllText(path, "@prefix x: <https://example.org/x#> .\nx:a x:b x:c .\nx:a x:b \"unterminated .\n");
        try
        {
            Run run = Deed("validate", path);

            Assert.Equal(2, run.ExitCode);
            Assert.Equal($"deed: {path}:3:24: does not parse: UnterminatedLiteral (byte 75){Environment.NewLine}", run.Error);
            Assert.Empty(run.Output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Help_exits_0()
    {
        Run run = Deed("--help");

        Assert.Equal(0, run.ExitCode);
        Assert.StartsWith("Usage: deed validate <map.ttl>...", run.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Model_writes_the_golden_model_to_standard_output()
    {
        Run run = Deed("model", "maps/varve/varve-acts.ttl");

        Assert.Equal(0, run.ExitCode);
        Assert.Equal(File.ReadAllText(Repository.File("tests/fixtures/varve.model.json")), run.Output);
        Assert.Empty(run.Error);
    }

    [Theory]
    [InlineData("--output")]
    [InlineData("--output=")]
    public void Model_writes_the_golden_model_to_a_file(string option)
    {
        string path = Path.Combine(Path.GetTempPath(), $"deed-{Guid.NewGuid():N}.json");
        try
        {
            Run run = option == "--output"
                ? Deed("model", "maps/product-cli/product-acts.ttl", option, path)
                : Deed("model", "maps/product-cli/product-acts.ttl", option + path);

            Assert.Equal(0, run.ExitCode);
            Assert.Empty(run.Output);
            Assert.Equal(File.ReadAllBytes(Repository.File("tests/fixtures/product-cli.model.json")), File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Model_on_a_map_with_violations_exits_1_and_writes_nothing()
    {
        string path = Path.Combine(Path.GetTempPath(), $"deed-{Guid.NewGuid():N}.json");
        Run run = Deed("model", "tests/fixtures/broken.ttl", "--output", path);

        Assert.Equal(1, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.False(File.Exists(path));
        Assert.Contains("22 violations, 0 warnings, 0 infos.", run.Error, StringComparison.Ordinal);
        Assert.EndsWith("no model is written." + Environment.NewLine, run.Error, StringComparison.Ordinal);

        Run toStandardOutput = Deed("model", "tests/fixtures/broken.ttl");
        Assert.Equal(1, toStandardOutput.ExitCode);
        Assert.Empty(toStandardOutput.Output);
    }

    [Fact]
    public void Model_on_maps_that_cannot_be_derived_exits_1_naming_both_terms_shortened()
    {
        // Varve and product-cli together declare two tools.
        Run run = Deed("model", "maps/varve/varve-acts.ttl", "maps/product-cli/product-acts.ttl");

        Assert.Equal(1, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.StartsWith("deed: cannot derive: The maps declare 2 tools: pc:Tool, varve:Tool.", run.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("model")]
    [InlineData("model", "maps/varve/varve-acts.ttl", "--output")]
    [InlineData("model", "maps/varve/varve-acts.ttl", "--output=")]
    [InlineData("model", "maps/varve/varve-acts.ttl", "--output", "a.json", "--output", "b.json")]
    [InlineData("validate", "maps/varve/varve-acts.ttl", "--output", "a.json")]
    public void Model_usage_errors_exit_2(params string[] args)
    {
        Run run = Deed(args);

        Assert.Equal(2, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Contains("deed model <map.ttl>... [--output <file>]", run.Error, StringComparison.Ordinal);
    }
}
