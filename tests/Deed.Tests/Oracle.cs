namespace Deed.Tests;

/// <summary>One row of <c>tests/fixtures/*.expected.tsv</c>, pySHACL's results.</summary>
internal sealed record OracleRow(string Severity, string Focus, string Shape, string Message)
{
    public bool ShapeIsBlank => Shape.StartsWith("_:", StringComparison.Ordinal);

    public static IReadOnlyList<OracleRow> Read(string fixture)
    {
        string[] lines = File.ReadAllLines(Repository.File($"tests/fixtures/{fixture}.expected.tsv"));
        Assert.Equal("severity\tfocus\tshape\tmessage", lines[0]);
        return [.. lines.Skip(1).Where(l => l.Length > 0).Select(l =>
        {
            string[] f = l.Split('\t');
            Assert.Equal(4, f.Length);
            return new OracleRow(f[0], f[1], f[2], f[3]);
        })];
    }
}
