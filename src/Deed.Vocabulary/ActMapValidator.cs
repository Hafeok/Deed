using System.Text;
using Varve.Rdf;
using Varve.Shacl;
using Varve.Shacl.Model;
using Varve.Shacl.Sparql;

namespace Deed.Vocabulary;

/// <summary>
/// Validates act maps against Deed's shapes.
/// </summary>
/// <remarks>
/// The data graph is Deed's act vocabulary, its CLI vocabulary and the act maps given, merged
/// into one default graph. The shapes graph is <c>deed-shapes.ttl</c>. SHACL-SPARQL is on; there
/// is no inference. Blank nodes are scoped to the document they appear in, as in any RDF merge.
/// </remarks>
public static class ActMapValidator
{
    private const string Sh = "http://www.w3.org/ns/shacl#";

    private static readonly ShaclOptions Options = new() { Extensions = [SparqlConstraints.Extension()] };

    private static readonly Lazy<ShapesGraph> Shapes = new(LoadShapes);

    /// <summary>Validates one or more act maps given as files.</summary>
    /// <exception cref="TurtleSyntaxException">A map does not parse.</exception>
    /// <exception cref="ValidationRefusedException">The engine cannot run the shapes.</exception>
    public static ValidationReport ValidateFiles(IEnumerable<string> paths) =>
        Validate(paths.Select(TurtleDocument.FromFile));

    /// <summary>Validates one or more act maps.</summary>
    /// <exception cref="TurtleSyntaxException">A map does not parse.</exception>
    /// <exception cref="ValidationRefusedException">The engine cannot run the shapes.</exception>
    public static ValidationReport Validate(IEnumerable<TurtleDocument> maps)
    {
        ArgumentNullException.ThrowIfNull(maps);
        return Validate(ActMapSet.Read(maps));
    }

    /// <summary>Validates act maps already read.</summary>
    /// <exception cref="ValidationRefusedException">The engine cannot run the shapes.</exception>
    public static ValidationReport Validate(ActMapSet maps)
    {
        ArgumentNullException.ThrowIfNull(maps);
        InMemoryDataset data = maps.Dataset;

        ValidationOutcome outcome = ShaclValidator.Validate(
            Shapes.Value, data, GraphPattern.DefaultGraph, Options, CancellationToken.None);
        if (!outcome.Succeeded || outcome.Report is null)
        {
            throw new ValidationRefusedException("validate", [.. outcome.Problems.Select(p => p.ToString())]);
        }

        List<ValidationResult> results = [.. outcome.Report.Results.Select(ToResult)];
        results.Sort(static (a, b) =>
        {
            int c = string.CompareOrdinal(a.FocusNode, b.FocusNode);
            if (c == 0) c = a.Severity.CompareTo(b.Severity);
            if (c == 0) c = string.CompareOrdinal(a.Message, b.Message);
            return c;
        });
        return new ValidationReport(results);
    }

    private static ShapesGraph LoadShapes()
    {
        InMemoryDataset shapes = ActMapSet.Parse([Vocabulary.Shapes], null, null, firstMap: 1);
        ShapesLoad load = ShapesGraph.Load(shapes, GraphPattern.DefaultGraph, Options, CancellationToken.None);
        if (!load.Succeeded || load.Shapes is null)
        {
            throw new ValidationRefusedException("load the shapes", [.. load.Problems.Select(p => p.ToString())]);
        }

        return load.Shapes;
    }

    private static ValidationResult ToResult(ShaclResult result) => new(
        ToSeverity(result.Severity),
        Render(result.FocusNode),
        Render(result.SourceShape),
        string.Join(Environment.NewLine, result.Messages.Select(m => Encoding.UTF8.GetString(m.Lexical))));

    private static Severity ToSeverity(RdfTerm severity) => Encoding.UTF8.GetString(severity.Lexical) switch
    {
        Sh + "Violation" => Severity.Violation,
        Sh + "Warning" => Severity.Warning,
        Sh + "Info" => Severity.Info,
        string other => throw new NotSupportedException($"Severity {other} is not one of SHACL's three."),
    };

    internal static string Render(RdfTerm term) => term.Kind switch
    {
        RdfTermKind.Iri => Encoding.UTF8.GetString(term.Lexical),
        RdfTermKind.BlankNode => "_:" + Encoding.UTF8.GetString(term.Lexical),
        RdfTermKind.Literal => "\"" + Encoding.UTF8.GetString(term.Lexical) + "\"",
        _ => "<< " + Render(term.Subject!) + " " + Render(term.Predicate!) + " " + Render(term.Object!) + " >>",
    };
}
