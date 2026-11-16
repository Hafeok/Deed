using System.Text;
using Deed.Vocabulary;
using Deed.Vocabulary.Model;

namespace Deed.Tests;

/// <summary>
/// Derivation rules on small maps written for one rule each. Each map validates with no violation,
/// so that what is refused is refused by the derivation and not by a shape.
/// </summary>
public sealed class DerivationTests
{
    /// <summary>A tool with one valid act; each test adds what it is about.</summary>
    private const string Base = """
        @prefix cli:  <https://w3id.org/deed/cli#> .
        @prefix deed: <https://w3id.org/deed/ns#> .
        @prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
        @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .
        @prefix t:    <https://example.org/t#> .

        t:Tool a cli:Tool ; cli:command "t" ; cli:defaultResource t:Box ; cli:defaultVerb t:show .
        t:Keeper a deed:Actor ; rdfs:label "keeper" ; rdfs:comment "Holds the key to the boxes." .
        t:Box a deed:Resource ; deed:name "box" ; rdfs:comment "A box." .
        t:Lid a deed:Resource ; deed:name "lid" ; rdfs:comment "The lid of a box." .
        t:show a deed:Verb ; deed:name "show" ; rdfs:comment "Give it as it is." .
        t:lid a deed:Verb ; deed:name "lid" ; rdfs:comment "Put a lid on it." .
        t:Contents a deed:State ; rdfs:comment "What is in the box. Read by keepers." .
        t:Thing a deed:Single .

        t:ShowLid a deed:Act ; rdfs:comment "Show the lid." ;
            deed:resource t:Lid ; deed:verb t:show ; deed:actor t:Keeper ;
            deed:input deed:Nothing ; deed:context deed:Nothing ; deed:needs t:Contents ;
            deed:changes deed:Nothing ; deed:outcome [ a deed:Single ; deed:yields t:LidView ] ;
            deed:failure deed:Nothing .

        """;

    private static ActMapSet Maps(string extra) =>
        ActMapSet.Read([new TurtleDocument("t.ttl", Encoding.UTF8.GetBytes(Base + extra))]);

    private static DerivationException Refused(string extra)
    {
        ActMapSet maps = Maps(extra);
        Assert.Equal(0, ActMapValidator.Validate(maps).Violations);
        return Assert.Throws<DerivationException>(() => ModelDeriver.Derive(maps));
    }

    [Fact]
    public void The_base_map_derives()
    {
        ActMapSet maps = Maps("");
        Assert.Equal(0, ActMapValidator.Validate(maps).Violations);

        ActModel act = Assert.Single(ModelDeriver.Derive(maps).Acts);
        Assert.Equal("t lid", act.Cli.Command!.Line);
    }

    [Fact]
    public void Two_acts_deriving_one_path_are_refused_naming_both()
    {
        // The default resource's verb 'lid' and the resource 'lid' under the default verb: both 't lid'.
        DerivationException e = Refused("""
            t:LidBox a deed:Act ; rdfs:comment "Put a lid on the box." ;
                deed:resource t:Box ; deed:verb t:lid ; deed:actor t:Keeper ;
                deed:input deed:Nothing ; deed:context deed:Nothing ; deed:needs deed:Nothing ;
                deed:changes deed:Nothing ; deed:outcome [ a deed:Single ; deed:yields t:LidView ] ;
                deed:failure deed:Nothing .
            """);

        DerivationError error = Assert.Single(e.Errors);
        Assert.Equal(["https://example.org/t#LidBox", "https://example.org/t#ShowLid"], error.Terms);
        Assert.Contains("'t lid'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_second_operand_bound_setting_is_refused_naming_both()
    {
        DerivationException e = Refused("""
            t:first a deed:Setting ; deed:name "first" ; deed:datatype xsd:string ; cli:bindAs cli:Operand ; rdfs:comment "The first." .
            t:second a deed:Setting ; deed:name "second" ; deed:datatype xsd:string ; cli:bindAs cli:Operand ; rdfs:comment "The second." .
            t:Place a deed:Context ; rdfs:comment "Where." ; deed:setting t:first , t:second ; deed:failure deed:Nothing .
            """);

        DerivationError error = Assert.Single(e.Errors);
        Assert.Equal(["https://example.org/t#first", "https://example.org/t#second"], error.Terms);
    }

    [Fact]
    public void An_environment_variable_on_an_operand_bound_setting_is_refused_naming_both()
    {
        DerivationException e = Refused("""
            t:where a deed:Setting ; deed:name "where" ; deed:datatype xsd:string ; cli:bindAs cli:Operand ; cli:env "T_WHERE" ; rdfs:comment "Where." .
            t:Place a deed:Context ; rdfs:comment "Where." ; deed:setting t:where ; deed:failure deed:Nothing .
            """);

        DerivationError error = Assert.Single(e.Errors);
        Assert.Equal(["https://example.org/t#where", "T_WHERE"], error.Terms);
    }

    [Fact]
    public void Two_terms_deriving_one_option_in_a_command_are_refused()
    {
        // Working position: not among issue #43's refusals. A parameter and a setting both named 'colour'.
        DerivationException e = Refused("""
            t:colour a deed:Parameter ; deed:name "colour" ; deed:role deed:Modifier ; deed:datatype xsd:string ; deed:required false ; rdfs:comment "The colour." .
            t:paint a deed:Setting ; deed:name "paint" ; cli:name "colour" ; deed:datatype xsd:string ; rdfs:comment "The paint." .
            t:Paint a deed:Context ; rdfs:comment "The paint pot." ; deed:setting t:paint ; deed:failure deed:Nothing .
            t:PaintBox a deed:Act ; rdfs:comment "Paint the box." ;
                deed:resource t:Box ; deed:verb t:show ; deed:actor t:Keeper ;
                deed:input ( t:colour ) ; deed:context t:Paint ; deed:needs deed:Nothing ;
                deed:changes deed:Nothing ; deed:outcome [ a deed:Single ; deed:yields t:LidView ] ;
                deed:failure deed:Nothing .
            """);

        DerivationError error = Assert.Single(e.Errors);
        Assert.Equal(["https://example.org/t#colour", "https://example.org/t#paint"], error.Terms.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Operands_are_the_bound_setting_then_subjects_then_modifiers_bound_as_operands()
    {
        // Rule 3. The modifier comes first in the input and still follows the subjects.
        ActMapSet maps = Maps("""
            t:where a deed:Setting ; deed:name "where" ; deed:datatype xsd:string ; cli:bindAs cli:Operand ; rdfs:comment "Where." .
            t:Place a deed:Context ; rdfs:comment "Where." ; deed:setting t:where ; deed:failure deed:Nothing .
            t:depth a deed:Parameter ; deed:name "depth" ; deed:role deed:Modifier ; deed:datatype xsd:integer ; deed:required false ; cli:bindAs cli:Operand ; rdfs:comment "How deep." .
            t:item a deed:Parameter ; deed:name "item" ; deed:role deed:Subject ; deed:datatype xsd:string ; deed:required true ; rdfs:comment "The item." .
            t:quiet a deed:Parameter ; deed:name "quiet" ; deed:role deed:Modifier ; deed:datatype xsd:boolean ; deed:required false ; cli:short "q" ; rdfs:comment "Say less." .
            t:FindInBox a deed:Act ; rdfs:comment "Find an item in the box." ;
                deed:resource t:Box ; deed:verb t:show ; deed:actor t:Keeper ;
                deed:input ( t:depth t:item t:quiet ) ; deed:context t:Place ; deed:needs t:Contents ;
                deed:changes deed:Nothing ; deed:outcome [ a deed:Stream ; deed:yields t:Item ] ;
                deed:failure deed:Nothing .
            """);
        Assert.Equal(0, ActMapValidator.Validate(maps).Violations);

        CliCommand command = ModelDeriver.Derive(maps).Acts.Single(a => a.Iri.EndsWith("#FindInBox", StringComparison.Ordinal)).Cli.Command!;
        Assert.Equal("t", command.Line);
        Assert.Equal(["where", "item", "depth"], command.Operands.Select(o => o.Name));
        CliFlag flag = Assert.Single(command.Flags);
        Assert.Equal(("quiet", "q"), (flag.Long, flag.Short));
        Assert.Empty(command.Options);
    }

    [Theory]
    [InlineData("deed:Nothing", true, false, true)]
    [InlineData("[ deed:state t:Contents ; deed:manner deed:Creates ]", false, false, false)]
    [InlineData("[ deed:state t:Contents ; deed:manner deed:Appends ]", false, false, false)]
    [InlineData("[ deed:state t:Contents ; deed:manner deed:Converges ]", false, true, true)]
    [InlineData("[ deed:state t:Contents ; deed:manner deed:Removes ]", false, true, true)]
    [InlineData("[ deed:state t:Contents ; deed:manner deed:Removes ] , [ deed:state t:Contents ; deed:manner deed:Appends ]", false, true, false)]
    public void Hints_follow_the_manners(string changes, bool readOnly, bool destructive, bool idempotent)
    {
        ActMapSet maps = Maps($$"""
            t:fill a deed:Verb ; deed:name "fill" ; rdfs:comment "Put things in it." .
            t:Gone a deed:Failure ; deed:name "gone" ; rdfs:comment "It was not there. Look again." .
            t:FillBox a deed:Act ; rdfs:comment "Fill the box." ;
                deed:resource t:Box ; deed:verb t:fill ; deed:actor t:Keeper ;
                deed:input deed:Nothing ; deed:context deed:Nothing ; deed:needs deed:Nothing ;
                deed:changes {{changes}} ; deed:outcome [ a deed:Single ; deed:yields t:LidView ] ;
                deed:failure t:Gone .
            """);
        Assert.Equal(0, ActMapValidator.Validate(maps).Violations);

        ActModel act = ModelDeriver.Derive(maps).Acts.Single(a => a.Iri.EndsWith("#FillBox", StringComparison.Ordinal));
        Assert.Equal(new HintsModel(readOnly, destructive, idempotent), act.Hints);
    }

    [Fact]
    public void A_failure_exits_1_unless_it_assigns_another_code()
    {
        ActMapSet maps = Maps("""
            t:Stuck a deed:Failure ; deed:name "stuck" ; cli:exitCode 4 ; rdfs:comment "The lid is stuck. Ask someone stronger." .
            t:Gone a deed:Failure ; deed:name "gone" ; rdfs:comment "It was not there. Look again." .
            t:Hinge a deed:Context ; rdfs:comment "The hinge." ; deed:setting t:angle ; deed:failure t:Stuck .
            t:angle a deed:Setting ; deed:name "angle" ; deed:datatype xsd:integer ; rdfs:comment "How far it opens." .
            t:OpenBox a deed:Act ; rdfs:comment "Open the box." ;
                deed:resource t:Box ; deed:verb t:show ; deed:actor t:Keeper ;
                deed:input deed:Nothing ; deed:context t:Hinge ; deed:needs t:Contents ;
                deed:changes deed:Nothing ; deed:outcome [ a deed:Single ; deed:yields t:LidView ] ;
                deed:failure t:Gone .
            """);
        Assert.Equal(0, ActMapValidator.Validate(maps).Violations);

        ActModel act = ModelDeriver.Derive(maps).Acts.Single(a => a.Iri.EndsWith("#OpenBox", StringComparison.Ordinal));
        Assert.Equal([("gone", 1, true), ("stuck", 4, false)], act.Failures.Select(f => (f.Name, f.ExitCode, f.Own)));
        Assert.Equal(["https://example.org/t#Hinge"], act.Failures.Single(f => f.Name == "stuck").Contexts);
    }

    [Fact]
    public void Printing_shortens_iris_under_declared_prefixes_and_keeps_a_sentence_end()
    {
        ActMapSet maps = Maps("");

        Assert.Equal("t:ShowLid and t:Lid. https://example.org/u#x",
            maps.Shorten("https://example.org/t#ShowLid and https://example.org/t#Lid. https://example.org/u#x"));
        Assert.Equal("deed:Nothing", maps.Shorten("https://w3id.org/deed/ns#Nothing"));
    }
}
