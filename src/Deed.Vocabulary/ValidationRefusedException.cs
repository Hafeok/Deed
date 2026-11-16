namespace Deed.Vocabulary;

/// <summary>
/// The SHACL engine refused to load the shapes or to validate, so there is no report.
/// This is not a finding about an act map: it means the engine cannot run a shape.
/// </summary>
public sealed class ValidationRefusedException : Exception
{
    internal ValidationRefusedException(string stage, IReadOnlyList<string> problems)
        : base($"The SHACL engine refused to {stage}:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}")
    {
        Problems = problems;
    }

    /// <summary>The engine's problems, one per entry.</summary>
    public IReadOnlyList<string> Problems { get; }
}
