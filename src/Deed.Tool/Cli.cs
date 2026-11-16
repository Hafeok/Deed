using Deed.Vocabulary;

namespace Deed.Tool;

/// <summary>
/// The command line, hand-written for this slice. It will be generated from an act map of
/// Deed itself once Deed can generate; until then it knows <c>validate</c> and nothing else.
/// </summary>
internal static class Cli
{
    public const int NoViolation = 0;
    public const int Violation = 1;
    public const int UsageError = 2;
    public const int EngineRefused = 3;

    private const string Usage = """
        Usage: deed validate <map.ttl>...

        Validates act maps against Deed's shapes and prints the report.
        The data graph is Deed's vocabulary, its CLI vocabulary and the maps given.

        Exit codes:
          0  no violation (warnings and infos do not fail)
          1  at least one violation
          2  usage error, or a file that cannot be read or does not parse
          3  the SHACL engine refused to run the shapes
        """;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        if (args.Count == 0)
        {
            return Fail(error, "no command given.");
        }

        if (IsHelp(args[0]))
        {
            output.WriteLine(Usage);
            return NoViolation;
        }

        if (args[0] != "validate")
        {
            return Fail(error, $"unknown command '{args[0]}'.");
        }

        List<string> files = [];
        bool optionsEnded = false;
        foreach (string arg in args.Skip(1))
        {
            if (!optionsEnded && arg == "--")
            {
                optionsEnded = true;
            }
            else if (!optionsEnded && IsHelp(arg))
            {
                output.WriteLine(Usage);
                return NoViolation;
            }
            else if (!optionsEnded && arg.StartsWith('-') && arg.Length > 1)
            {
                return Fail(error, $"unknown option '{arg}'.");
            }
            else
            {
                files.Add(arg);
            }
        }

        if (files.Count == 0)
        {
            return Fail(error, "validate needs at least one act map.");
        }

        List<TurtleDocument> maps = [];
        foreach (string file in files)
        {
            try
            {
                maps.Add(TurtleDocument.FromFile(file));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                error.WriteLine($"deed: {file}: cannot be read: {e.Message}");
                return UsageError;
            }
        }

        ValidationReport report;
        try
        {
            report = ActMapValidator.Validate(maps);
        }
        catch (TurtleSyntaxException e)
        {
            error.WriteLine($"deed: {e.Document}:{e.Line}:{e.Column}: does not parse: {e.Kind} (byte {e.ByteOffset})");
            return UsageError;
        }
        catch (ValidationRefusedException e)
        {
            error.WriteLine($"deed: {e.Message}");
            return EngineRefused;
        }

        ReportWriter.Write(report, output);
        return report.HasNoViolation ? NoViolation : Violation;
    }

    private static bool IsHelp(string arg) => arg is "-h" or "--help" or "help";

    private static int Fail(TextWriter error, string message)
    {
        error.WriteLine($"deed: {message}");
        error.WriteLine();
        error.WriteLine(Usage);
        return UsageError;
    }
}
