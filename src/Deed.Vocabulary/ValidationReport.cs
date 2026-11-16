namespace Deed.Vocabulary;

/// <summary>What validation found, as data.</summary>
public sealed class ValidationReport
{
    internal ValidationReport(IReadOnlyList<ValidationResult> results) => Results = results;

    /// <summary>Every result, ordered by focus node, then severity, then message.</summary>
    public IReadOnlyList<ValidationResult> Results { get; }

    /// <summary>The number of violations.</summary>
    public int Violations => Count(Severity.Violation);

    /// <summary>The number of warnings.</summary>
    public int Warnings => Count(Severity.Warning);

    /// <summary>The number of infos.</summary>
    public int Infos => Count(Severity.Info);

    /// <summary>True when there is no violation. Warnings and infos do not count against it.</summary>
    public bool HasNoViolation => Violations == 0;

    /// <summary>The number of results of one severity.</summary>
    public int Count(Severity severity) => Results.Count(r => r.Severity == severity);
}
