using Deed.Vocabulary;

namespace Deed.Tool;

/// <summary>Prints a report: one entry per result, grouped by focus node, then counts by severity.</summary>
internal static class ReportWriter
{
    public static void Write(ValidationReport report, TextWriter output)
    {
        foreach (IGrouping<string, ValidationResult> node in report.Results.GroupBy(r => r.FocusNode))
        {
            output.WriteLine(node.Key);
            foreach (ValidationResult result in node)
            {
                output.WriteLine($"  {result.Severity}: {result.Message}");
                if (!result.SourceShapeIsBlank)
                {
                    output.WriteLine($"    (shape {result.SourceShape})");
                }
            }

            output.WriteLine();
        }

        output.WriteLine(
            $"{Counted(report.Violations, "violation")}, {Counted(report.Warnings, "warning")}, {Counted(report.Infos, "info")}.");
    }

    private static string Counted(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}
