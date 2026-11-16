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

    /// <summary>
    /// The messages in <c>deed-shapes.ttl</c> that no result on the broken fixture carries,
    /// under Varve.Shacl and, by the oracle, under pySHACL alike. Issue #33 expected none;
    /// the fixture does not reach them (#36). Listed here so that a change is seen, not to excuse it.
    /// </summary>
    private static readonly string[] NotReachedByTheFixture =
    [
        "An act is about exactly one resource. What is th", // dsh:ActIdentity
        "An act has exactly one verb from the controlled ", // dsh:ActIdentity
        "The input is an ordered list of parameters, or d", // dsh:ActInput
        "Everything in an act's input is a parameter defi", // dsh:ActInput
        "A context is declared once for the tool, with it", // dsh:ActContext
        "This act says it has no context and also names o", // dsh:ActContextNothingAlone
        "What an act needs is a named state of the tool. ", // dsh:ActNeeds
        "A change names one state and the manner it is wr", // dsh:ActChanges
        "This act says it changes nothing and also names ", // dsh:ActChangesNothingAlone
        "Is the outcome complete when the act ends (deed:", // dsh:ActOutcome
        "A failure is declared once for the tool, with a ", // dsh:ActFailures
        "This act says it cannot fail on its own account ", // dsh:ActFailuresNothingAlone
        "A resource has one name: a singular noun, lower ", // dsh:Resource
        "What is this resource, in one sentence? If it ca", // dsh:Resource
        "A resource is part of at most one other. If it s", // dsh:Resource
        "A verb has one name: a single verb in the impera", // dsh:Verb
        "What does this verb mean on any resource? If the", // dsh:Verb
        "What is this role called by the people who grant", // dsh:Actor
        "What makes someone this actor: a grant, a creden", // dsh:Actor
        "A parameter has one name, lower case, hyphenated", // dsh:Parameter
        "Is this parameter what the act is performed on (", // dsh:Parameter
        "What kind of value is this? If it is the content", // dsh:Parameter
        "Can the act proceed without this parameter? Say ", // dsh:Parameter
        "A yes-or-no value says how an act is done, never", // dsh:SubjectIsNotAFlag
        "What does this context stand for, and for how lo", // dsh:Context
        "What must the environment supply for this contex", // dsh:Context
        "How does this context fail every act that uses i", // dsh:Context
        "A setting has one name, lower case, hyphenated. ", // dsh:Setting
        "What kind of value does the environment supply h", // dsh:Setting
        "What is this setting, and what happens when the ", // dsh:Setting
        "What is this state, and who may read it and who ", // dsh:State
        "A failure has one name that a program can match ", // dsh:Failure
        "What should the actor do next when this happens:", // dsh:Failure
        "What does the user type to run this tool? One lo", // dsh:Tool
        "Only one resource can go unnamed on the command ", // dsh:Tool
        "Only one verb can go unsaid on the command line.", // dsh:Tool
        "A short name is one letter or digit, and a tool ", // dsh:Short
        "Only a failure has an exit code of its own. If s", // dsh:ExitCode
        "A failure exits 1 unless it says otherwise, and ", // dsh:ExitCode
        "This is bound against its rule: a subject as an ", // dsh:BindAs
        "This is spelled differently on the command line ", // dsh:CliName
        "This act's command line is recorded, not derived", // dsh:SpelledAs
        "Why does only the command line need this option?", // dsh:SurfaceOption
        "What does this command do until it is stopped? I", // dsh:Host
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

    [Fact]
    public void Messages_the_broken_fixture_does_not_reach()
    {
        List<string> notReached = [.. MessagesInShapes()
            .Where(t => !Broken.Results.Any(r => Matches(t, r.Message)))
            .Select(t => t[..Math.Min(48, t.Length)])];

        Assert.Equal(NotReachedByTheFixture, notReached);
        Assert.Equal(61, MessagesInShapes().Count);
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
