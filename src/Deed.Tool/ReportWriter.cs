using Deed.Vocabulary;

namespace Deed.Tool;

/// <summary>
/// Prints a report: one entry per result, grouped by focus node, then counts by severity.
/// IRIs are shortened with the prefixes the maps declare (ADR 0022); the report itself keeps them whole.
/// </summary>
internal static class ReportWriter
{
    public static void Write(ValidationReport report, ActMapSet maps, TextWriter output)
    {
        foreach (IGrouping<string, ValidationResult> node in report.Results.GroupBy(r => r.FocusNode))
        {
            output.WriteLine(maps.Shorten(node.Key));
            foreach (ValidationResult result in node)
            {
                output.WriteLine($"  {result.Severity}: {maps.Shorten(result.Message)}");
                if (!result.SourceShapeIsBlank)
                {
                    output.WriteLine($"    (shape {maps.Shorten(result.SourceShape)})");
                }
            }

            output.WriteLine();
        }

        output.WriteLine(
            $"{Counted(report.Violations, "violation")}, {Counted(report.Warnings, "warning")}, {Counted(report.Infos, "info")}.");
    }

    private static string Counted(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}
