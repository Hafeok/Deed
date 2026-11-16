using System.Text;
using Deed.Vocabulary;

namespace Deed.Tests;

/// <summary>
/// The SHACL features issue #12 asks about that the broken fixture does not exercise.
/// These inputs are not fixtures and have no oracle: each expectation follows from the
/// SHACL specification and the shape as written, and each test asserts only the result it is about.
/// </summary>
public sealed class EngineFeatureTests
{
    private const string Prefixes = """
        @prefix deed: <https://w3id.org/deed/ns#> .
        @prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
        @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .
        @prefix x:    <https://example.org/x#> .

        """;

    private static ValidationReport Validate(params string[] documents) =>
        ActMapValidator.Validate(documents.Select((d, i) => new TurtleDocument($"doc{i}.ttl", Encoding.UTF8.GetBytes(Prefixes + d))));

    private static IEnumerable<ValidationResult> About(ValidationReport report, string focus, string messageStart) =>
        report.Results.Where(r => r.FocusNode == "https://example.org/x#" + focus && r.Message.StartsWith(messageStart, StringComparison.Ordinal));

    [Fact]
    public void Sequence_path_with_zero_or_more_reaches_every_list_element()
    {
        // ( deed:input [ sh:zeroOrMorePath rdf:rest ] rdf:first ) with sh:class deed:Parameter:
        // one result per value node, so both elements, the second only through rdf:rest.
        ValidationReport report = Validate("x:A a deed:Act ; deed:input ( x:one x:two ) .");

        List<ValidationResult> results = [.. About(report, "A", "Everything in an act's input is a parameter")];
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(Severity.Violation, r.Severity));
    }

    [Fact]
    public void Or_of_in_and_node_refuses_a_value_that_is_neither()
    {
        // sh:or ( [ sh:in ( deed:Nothing ) ] [ sh:node dsh:NonEmptyList ] ).
        ValidationReport accepted = Validate("x:A a deed:Act ; deed:input deed:Nothing .");
        ValidationReport refused = Validate("x:A a deed:Act ; deed:input x:notAList .");

        Assert.Empty(About(accepted, "A", "The input is an ordered list"));
        Assert.Single(About(refused, "A", "The input is an ordered list"));
    }

    [Fact]
    public void Not_of_and_refuses_a_boolean_subject()
    {
        // dsh:SubjectIsNotAFlag: sh:not [ sh:and ( role Subject, datatype boolean ) ].
        ValidationReport report = Validate("""
            x:yes a deed:Parameter ; deed:role deed:Subject ; deed:datatype xsd:boolean .
            x:no  a deed:Parameter ; deed:role deed:Modifier ; deed:datatype xsd:boolean .
            """);

        Assert.Single(About(report, "yes", "A yes-or-no value says how an act is done"));
        Assert.Empty(About(report, "no", "A yes-or-no value says how an act is done"));
    }

    [Fact]
    public void Node_shape_inside_or_checks_the_manner()
    {
        // sh:or ( [ sh:node dsh:Change ] [ sh:in ( deed:Nothing ) ] ), dsh:Change requiring sh:in on the manner.
        ValidationReport report = Validate("""
            x:S a deed:State .
            x:Good a deed:Act ; deed:changes [ deed:state x:S ; deed:manner deed:Creates ] .
            x:Bad  a deed:Act ; deed:changes [ deed:state x:S ; deed:manner x:Overwrites ] .
            """);

        Assert.Empty(About(report, "Good", "A change names one state"));
        Assert.Single(About(report, "Bad", "A change names one state"));
    }

    [Fact]
    public void Blank_nodes_are_scoped_to_their_document()
    {
        // The same label in two documents names two nodes (RDF merge). If they were one node,
        // x:B's change would borrow x:A's state and manner and pass.
        ValidationReport report = Validate(
            "x:S a deed:State . x:A a deed:Act ; deed:changes _:c . _:c deed:state x:S ; deed:manner deed:Creates .",
            "x:B a deed:Act ; deed:changes _:c . _:c rdfs:comment \"no state, no manner\" .");

        Assert.Empty(About(report, "A", "A change names one state"));
        Assert.Single(About(report, "B", "A change names one state"));
    }
}
