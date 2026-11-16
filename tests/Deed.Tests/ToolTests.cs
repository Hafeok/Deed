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
    public void Results_are_grouped_by_focus_node_with_severity_and_full_message()
    {
        Run run = Deed("validate", "tests/fixtures/broken.ttl");

        // The entry for x:force: its heading once, then its results until a blank line.
        string[] lines = run.Output.Split(Environment.NewLine);
        Assert.Equal(1, lines.Count(l => l == "https://example.org/x#force"));
        string[] entry = [.. lines.SkipWhile(l => l != "https://example.org/x#force").Skip(1).TakeWhile(l => l.Length > 0)];

        string[] expected =
        [
            "  Violation: 'force' is also the name of https://example.org/x#path. A name means one thing across the tool. Are these the same parameter, or does one of them need a more exact name?",
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
}
