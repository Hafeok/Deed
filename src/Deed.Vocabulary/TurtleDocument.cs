namespace Deed.Vocabulary;

/// <summary>A Turtle document to be read: its name, for messages, and its bytes.</summary>
/// <param name="Name">How the document is named in a report or an error: usually its path.</param>
/// <param name="Content">The document, UTF-8.</param>
/// <param name="BaseIri">The IRI relative IRIs resolve against, or null for none.</param>
public sealed record TurtleDocument(string Name, ReadOnlyMemory<byte> Content, string? BaseIri = null)
{
    /// <summary>Reads a document from a file. Relative IRIs resolve against the file's URI.</summary>
    public static TurtleDocument FromFile(string path)
    {
        string full = Path.GetFullPath(path);
        return new TurtleDocument(path, File.ReadAllBytes(full), new Uri(full).AbsoluteUri);
    }
}
