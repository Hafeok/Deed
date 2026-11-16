using Deed.Vocabulary;

namespace Deed.Tests;

/// <summary>Varve.Shacl against the oracle: pySHACL's results in <c>tests/fixtures</c>.</summary>
public sealed class OracleTests
{
    public static TheoryData<string, string, int, int, int> Inputs => new()
    {
        { "maps/varve/varve-acts.ttl", "varve", 0, 10, 8 },
        { "maps/product-cli/product-acts.ttl", "product-cli", 0, 2, 4 },
        { "tests/fixtures/broken.ttl", "broken", 22, 0, 0 },
        { "tests/fixtures/broken-terms.ttl", "broken-terms", 47, 0, 0 },
    };

    [Theory]
    [MemberData(nameof(Inputs))]
    public void Counts_by_severity(string map, string fixture, int violations, int warnings, int infos)
    {
        _ = fixture;
        ValidationReport report = ActMapValidator.ValidateFiles([Repository.File(map)]);

        Assert.Equal(violations, report.Violations);
        Assert.Equal(warnings, report.Warnings);
        Assert.Equal(infos, report.Infos);
        Assert.Equal(violations + warnings + infos, report.Results.Count);
    }

    /// <summary>
    /// Each result matches the oracle on severity, focus node and message, and on the source
    /// shape where it is named. A blank-node shape matches any blank-node shape.
    /// </summary>
    [Theory]
    [MemberData(nameof(Inputs))]
    public void Results_match_the_oracle(string map, string fixture, int violations, int warnings, int infos)
    {
        _ = (violations, warnings, infos);
        ValidationReport report = ActMapValidator.ValidateFiles([Repository.File(map)]);

        List<string> expected = [.. OracleRow.Read(fixture).Select(r => Key(r.Severity, r.Focus, r.ShapeIsBlank ? "_:" : r.Shape, r.Message)).Order(StringComparer.Ordinal)];
        List<string> actual = [.. report.Results.Select(r => Key(r.Severity.ToString(), r.FocusNode, r.SourceShapeIsBlank ? "_:" : r.SourceShape, r.Message)).Order(StringComparer.Ordinal)];

        Assert.Equal(expected, actual);
    }

    private static string Key(string severity, string focus, string shape, string message) =>
        $"{severity} | {focus} | {shape} | {message}";
}
