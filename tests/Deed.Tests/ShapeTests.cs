using System.Text.RegularExpressions;
using Deed.Vocabulary;

namespace Deed.Tests;

/// <summary>What the broken fixture exercises in <c>deed-shapes.ttl</c>.</summary>
public sealed partial class ShapeTests
{
    private static readonly ValidationReport Broken =
        ActMapValidator.ValidateFiles([Repository.File("tests/fixtures/broken.ttl")]);

    [Theory]
    [InlineData("https://w3id.org/deed/shapes#ActThatChangesCanFail", "https://example.org/x#TakeLease",
        "This act changes https://example.org/x#LeaseState but declares no failure of its own. What happens when https://example.org/x#LeaseState is not as the actor expects: already there, already held, moved on since they looked?")]
    [InlineData("https://w3id.org/deed/shapes#ActIsUnique", "https://example.org/x#TakeLease",
        "This act and https://example.org/x#GrabLease have the same resource and the same verb. Are they one act with a parameter, or is one of them about a resource that has not been named?")]
    [InlineData("https://w3id.org/deed/shapes#ActIsUnique", "https://example.org/x#GrabLease",
        "This act and https://example.org/x#TakeLease have the same resource and the same verb. Are they one act with a parameter, or is one of them about a resource that has not been named?")]
    [InlineData("https://w3id.org/deed/shapes#NameMeansOneThing", "https://example.org/x#force",
        "'force' is also the name of https://example.org/x#path. A name means one thing across the tool. Are these the same parameter, or does one of them need a more exact name?")]
    [InlineData("https://w3id.org/deed/shapes#NameMeansOneThing", "https://example.org/x#path",
        "'force' is also the name of https://example.org/x#force. A name means one thing across the tool. Are these the same parameter, or does one of them need a more exact name?")]
    public void Sparql_messages_substitute_their_variables(string shape, string focus, string message)
    {
        ValidationResult result = Assert.Single(Broken.Results, r => r.SourceShape == shape && r.FocusNode == focus);
        Assert.Equal(message, result.Message);
        Assert.DoesNotContain("{?", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void No_message_is_left_with_a_variable()
    {
        Assert.All(Broken.Results, r => Assert.DoesNotContain("{?", r.Message, StringComparison.Ordinal));
    }

    /// <summary>The fixtures whose results, together, carry every message (issue #36).</summary>
    private static readonly string[] Fixtures =
    [
        "maps/varve/varve-acts.ttl",
        "maps/product-cli/product-acts.ttl",
        "tests/fixtures/broken.ttl",
        "tests/fixtures/broken-terms.ttl",
    ];

    [Fact]
    public void Messages_the_broken_fixture_reaches_are_the_ones_the_oracle_reaches()
    {
        List<string> templates = MessagesInShapes();
        HashSet<string> oracle = [.. OracleRow.Read("broken").Select(r => r.Message)];

        List<string> reachedByVarve = [.. templates.Where(t => Broken.Results.Any(r => Matches(t, r.Message)))];
        List<string> reachedByOracle = [.. templates.Where(t => oracle.Any(m => Matches(t, m)))];

        Assert.Equal(reachedByOracle, reachedByVarve);
    }

    /// <summary>
    /// Every message in <c>deed-shapes.ttl</c> is carried by a result on some fixture, so that no
    /// shape can stop working in the engine without a test failing.
    /// </summary>
    [Fact]
    public void Every_message_in_the_shapes_is_reached_by_some_fixture()
    {
        List<string> templates = MessagesInShapes();
        List<ValidationResult> results = [.. Fixtures.SelectMany(f => ActMapValidator.ValidateFiles([Repository.File(f)]).Results)];

        List<string> notReached = [.. templates.Where(t => !results.Any(r => Matches(t, r.Message)))];

        Assert.Empty(notReached);
        Assert.Equal(61, templates.Count);
    }

    private static List<string> MessagesInShapes()
    {
        string shapes = File.ReadAllText(Repository.File("vocabulary/deed-shapes.ttl"));
        return [.. MessagePattern().Matches(shapes).Select(m => m.Groups[1].Value).Distinct()];
    }

    private static bool Matches(string template, string message) =>
        Regex.IsMatch(message, "^" + Variable().Replace(Regex.Escape(template), ".+") + "$", RegexOptions.Singleline);

    [GeneratedRegex("""sh:message\s+"((?:[^"\\]|\\.)*)"(?!")""")]
    private static partial Regex MessagePattern();

    [GeneratedRegex(@"\\\{\\\?\w+}")]
    private static partial Regex Variable();
}
