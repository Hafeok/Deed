using System.Reflection;

namespace Deed.Vocabulary;

/// <summary>Deed's own Turtle, as carried in this assembly.</summary>
public static class Vocabulary
{
    /// <summary>The act layer: <c>vocabulary/deed.ttl</c>.</summary>
    public static TurtleDocument Act { get; } = Load("deed.ttl");

    /// <summary>The CLI binding layer: <c>vocabulary/deed-cli.ttl</c>.</summary>
    public static TurtleDocument Cli { get; } = Load("deed-cli.ttl");

    /// <summary>The shapes: <c>vocabulary/deed-shapes.ttl</c>.</summary>
    public static TurtleDocument Shapes { get; } = Load("deed-shapes.ttl");

    private static TurtleDocument Load(string file)
    {
        string resource = "Deed.Vocabulary." + file;
        using Stream stream = typeof(Vocabulary).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The assembly does not carry {resource}.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return new TurtleDocument("vocabulary/" + file, buffer.ToArray());
    }
}
