namespace Deed.Vocabulary;

/// <summary>How much a result matters. SHACL's three severities.</summary>
public enum Severity
{
    /// <summary><c>sh:Violation</c>: a question that must be answered before anything is generated.</summary>
    Violation,

    /// <summary><c>sh:Warning</c>.</summary>
    Warning,

    /// <summary><c>sh:Info</c>.</summary>
    Info,
}

/// <summary>One validation result.</summary>
/// <param name="Severity">The result's severity.</param>
/// <param name="FocusNode">The node the result is about: an IRI, or a blank node as <c>_:label</c>.</param>
/// <param name="SourceShape">The shape that produced it: an IRI, or a blank node as <c>_:label</c>. A blank node's label is the engine's and is stable only within one run.</param>
/// <param name="Message">The shape's message in full, with SHACL-SPARQL variables substituted.</param>
public sealed record ValidationResult(Severity Severity, string FocusNode, string SourceShape, string Message)
{
    /// <summary>Whether the source shape is a blank node (an unnamed property shape).</summary>
    public bool SourceShapeIsBlank => SourceShape.StartsWith("_:", StringComparison.Ordinal);
}
