using Deed.Vocabulary;
using Deed.Vocabulary.Model;

namespace Deed.Tool;

/// <summary>
/// The command line, hand-written until Deed can generate its own. Exit codes follow ADR 0020:
/// 0 success, 1 a map with violations or one that cannot be derived, 2 anything that prevents
/// validating (a usage error, a file that cannot be read or does not parse, an engine that refuses the shapes).
/// </summary>
internal static class Cli
{
    public const int Success = 0;
    public const int Refused = 1;
    public const int UsageError = 2;

    private const string Usage = """
        Usage: deed validate <map.ttl>...
               deed model <map.ttl>... [--output <file>]

        validate  Validates act maps against Deed's shapes and prints the report.
        model     Validates act maps, derives the surfaces and writes the model file
                  (JSON, docs/model.md) to standard output or to --output.

        The data graph is Deed's vocabulary, its CLI vocabulary and the maps given.
        Printed IRIs are shortened with the prefixes the maps declare.

        Exit codes:
          0  no violation (warnings and infos do not fail); for model, the model was written
          1  at least one violation, or the maps cannot be derived; no model is written
          2  usage error, a file that cannot be read or does not parse,
             or a SHACL engine that refuses to run the shapes
        """;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error, Func<Stream>? stdout = null)
    {
        if (args.Count == 0)
        {
            return Fail(error, "no command given.");
        }

        if (IsHelp(args[0]))
        {
            output.WriteLine(Usage);
            return Success;
        }

        string command = args[0];
        if (command is not ("validate" or "model"))
        {
            return Fail(error, $"unknown command '{command}'.");
        }

        List<string> files = [];
        string? outputFile = null;
        bool optionsEnded = false;
        for (int i = 1; i < args.Count; i++)
        {
            string arg = args[i];
            if (!optionsEnded && arg == "--")
            {
                optionsEnded = true;
            }
            else if (!optionsEnded && IsHelp(arg))
            {
                output.WriteLine(Usage);
                return Success;
            }
            else if (!optionsEnded && command == "model" && (arg == "--output" || arg.StartsWith("--output=", StringComparison.Ordinal)))
            {
                if (outputFile is not null)
                {
                    return Fail(error, "--output is given twice.");
                }

                if (arg == "--output")
                {
                    if (i + 1 >= args.Count)
                    {
                        return Fail(error, "--output needs a file.");
                    }

                    outputFile = args[++i];
                }
                else
                {
                    outputFile = arg["--output=".Length..];
                }

                if (outputFile.Length == 0)
                {
                    return Fail(error, "--output needs a file.");
                }
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
            return Fail(error, $"{command} needs at least one act map.");
        }

        List<TurtleDocument> documents = [];
        foreach (string file in files)
        {
            try
            {
                documents.Add(TurtleDocument.FromFile(file));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                error.WriteLine($"deed: {file}: cannot be read: {e.Message}");
                return UsageError;
            }
        }

        ActMapSet maps;
        ValidationReport report;
        try
        {
            maps = ActMapSet.Read(documents);
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
            return UsageError;
        }

        if (command == "validate")
        {
            ReportWriter.Write(report, maps, output);
            return report.HasNoViolation ? Success : Refused;
        }

        // model: the report goes to standard error, since standard output may be the model.
        if (!report.HasNoViolation)
        {
            ReportWriter.Write(report, maps, error);
            error.WriteLine("deed: the maps have violations; no model is written.");
            return Refused;
        }

        DeedModel model;
        try
        {
            model = ModelDeriver.Derive(maps);
        }
        catch (DerivationException e)
        {
            foreach (DerivationError refusal in e.Errors)
            {
                error.WriteLine($"deed: cannot derive: {maps.Shorten(refusal.Message)}");
            }

            error.WriteLine("deed: no model is written.");
            return Refused;
        }

        byte[] json = ModelWriter.ToUtf8(model);
        if (outputFile is null)
        {
            output.Flush();
            using Stream stream = (stdout ?? Console.OpenStandardOutput)();
            stream.Write(json);
            stream.Flush();
            return Success;
        }

        try
        {
            File.WriteAllBytes(outputFile, json);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error.WriteLine($"deed: {outputFile}: cannot be written: {e.Message}");
            return UsageError;
        }

        return Success;
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
