namespace Deed.Vocabulary;

/// <summary>A document did not parse as Turtle. Carries the parser's position.</summary>
public sealed class TurtleSyntaxException : Exception
{
    internal TurtleSyntaxException(string document, int line, int column, long byteOffset, string kind)
        : base($"{document}:{line}:{column}: {kind}")
    {
        Document = document;
        Line = line;
        Column = column;
        ByteOffset = byteOffset;
        Kind = kind;
    }

    /// <summary>The document's name, as given.</summary>
    public string Document { get; }

    /// <summary>The line, counting from one.</summary>
    public int Line { get; }

    /// <summary>The byte within the line, counting from one.</summary>
    public int Column { get; }

    /// <summary>The byte offset in the document, counting from zero.</summary>
    public long ByteOffset { get; }

    /// <summary>The parser's name for what went wrong, for example <c>UndeclaredPrefix</c>.</summary>
    public string Kind { get; }
}
