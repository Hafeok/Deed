using System.Text;
using System.Text.RegularExpressions;
using Varve.Rdf;
using Varve.Turtle;

namespace Deed.Vocabulary;

/// <summary>
/// Act maps read together with Deed's vocabulary into one graph: what validation runs over and
/// what the model is derived from. Also keeps the prefixes the maps declare, for printing.
/// </summary>
public sealed partial class ActMapSet
{
    private readonly Dictionary<(Term Subject, string Predicate), List<Term>> _objects;
    private readonly Dictionary<(string Predicate, Term Object), List<Term>> _subjects;
    private readonly List<KeyValuePair<string, string>> _shortening;

    private ActMapSet(InMemoryDataset dataset, List<(Term S, string P, Term O)> triples, IReadOnlyList<KeyValuePair<string, string>> prefixes)
    {
        Dataset = dataset;
        Prefixes = prefixes;
        _objects = [];
        _subjects = [];
        foreach ((Term s, string p, Term o) in triples)
        {
            Add(_objects, (s, p), o);
            Add(_subjects, (p, o), s);
        }

        _shortening = ShorteningTable(prefixes);
    }

    /// <summary>The prefix bindings the act maps declare, in document order: prefix (no colon), namespace.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Prefixes { get; }

    internal InMemoryDataset Dataset { get; }

    /// <summary>Reads act maps given as files, with Deed's vocabulary.</summary>
    /// <exception cref="TurtleSyntaxException">A map does not parse.</exception>
    public static ActMapSet ReadFiles(IEnumerable<string> paths) => Read(paths.Select(TurtleDocument.FromFile));

    /// <summary>Reads act maps, with Deed's vocabulary.</summary>
    /// <exception cref="TurtleSyntaxException">A map does not parse.</exception>
    public static ActMapSet Read(IEnumerable<TurtleDocument> maps)
    {
        ArgumentNullException.ThrowIfNull(maps);
        List<TurtleDocument> documents = [Vocabulary.Act, Vocabulary.Cli, .. maps];
        List<(Term, string, Term)> triples = [];
        List<KeyValuePair<string, string>> prefixes = [];
        InMemoryDataset dataset = Parse(documents, triples, prefixes, firstMap: 2);
        return new ActMapSet(dataset, triples, prefixes);
    }

    /// <summary>Parses documents into one dataset, each document's blank nodes kept apart.</summary>
    internal static InMemoryDataset Parse(
        IReadOnlyList<TurtleDocument> documents,
        List<(Term, string, Term)>? triples,
        List<KeyValuePair<string, string>>? prefixes,
        int firstMap)
    {
        InMemoryDatasetBuilder builder = new();
        for (int i = 0; i < documents.Count; i++)
        {
            TurtleDocument document = documents[i];
            string scope = $"d{i}-";
            bool isMap = i >= firstMap;
            TurtleOptions options = new()
            {
                Syntax = RdfSyntax.Turtle,
                BaseIri = document.BaseIri is null ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(document.BaseIri),
                OnPrefix = prefixes is null || !isMap
                    ? null
                    : (prefix, ns) => prefixes.Add(new(Encoding.UTF8.GetString(prefix), Encoding.UTF8.GetString(ns))),
            };

            ParseResult parsed = TurtleParser.Parse(
                document.Content.Span,
                (in QuadView quad) =>
                {
                    RdfTerm s = Scoped(quad.Subject.Materialise(), scope);
                    RdfTerm p = Scoped(quad.Predicate.Materialise(), scope);
                    RdfTerm o = Scoped(quad.Object.Materialise(), scope);
                    builder.Add(s, p, o, null);
                    triples?.Add((Term.From(s), Encoding.UTF8.GetString(p.Lexical), Term.From(o)));
                },
                in options);

            if (!parsed.Succeeded)
            {
                var error = parsed.FirstError;
                throw new TurtleSyntaxException(
                    document.Name, error.Position.Line, error.Position.Column, error.Position.ByteOffset, error.Kind.ToString());
            }
        }

        return builder.ToDataset();
    }

    private static RdfTerm Scoped(RdfTerm term, string scope) => term.Kind switch
    {
        RdfTermKind.BlankNode => RdfTerm.BlankNode(Encoding.UTF8.GetBytes(scope + Encoding.UTF8.GetString(term.Lexical))),
        RdfTermKind.TripleTerm => RdfTerm.TripleTerm(
            Scoped(term.Subject!, scope), Scoped(term.Predicate!, scope), Scoped(term.Object!, scope)),
        _ => term,
    };

    // ------------------------------------------------------------------ queries

    internal IReadOnlyList<Term> Objects(Term subject, string predicate) =>
        _objects.TryGetValue((subject, predicate), out List<Term>? o) ? o : [];

    internal IReadOnlyList<Term> Subjects(string predicate, Term @object) =>
        _subjects.TryGetValue((predicate, @object), out List<Term>? s) ? s : [];

    internal Term? Object(Term subject, string predicate)
    {
        IReadOnlyList<Term> all = Objects(subject, predicate);
        return all.Count == 0 ? null : all[0];
    }

    internal string? Text(Term subject, string predicate) => Object(subject, predicate)?.Value;

    internal bool Is(Term subject, string type) => Objects(subject, Iris.Type).Contains(Term.Iri(type));

    internal IEnumerable<Term> OfType(string type) =>
        Subjects(Iris.Type, Term.Iri(type)).Distinct().OrderBy(t => t.Value, StringComparer.Ordinal);

    /// <summary>The members of an RDF list, in order.</summary>
    internal IReadOnlyList<Term> List(Term head)
    {
        List<Term> items = [];
        HashSet<Term> seen = [];
        Term? node = head;
        while (node is { } n && n != Term.Iri(Iris.Nil) && seen.Add(n))
        {
            if (Object(n, Iris.First) is { } first)
            {
                items.Add(first);
            }

            node = Object(n, Iris.Rest);
        }

        return items;
    }

    private static void Add<TKey>(Dictionary<TKey, List<Term>> index, TKey key, Term value)
        where TKey : notnull
    {
        if (!index.TryGetValue(key, out List<Term>? list))
        {
            index[key] = list = [];
        }

        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }

    // ----------------------------------------------------------------- printing

    /// <summary>
    /// Shortens every IRI in a text that falls under a prefix the maps declare (ADR 0022).
    /// An IRI under no declared prefix is left whole. For printing only: results as data keep full IRIs.
    /// </summary>
    public string Shorten(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach ((string ns, string prefix) in _shortening)
        {
            Regex pattern = new(Regex.Escape(ns) + LocalName, RegexOptions.CultureInvariant);
            text = pattern.Replace(text, m => prefix + ":" + m.Groups["local"].Value);
        }

        return text;
    }

    // A local name: letters, digits, '_' and '-', with inner dots; never ending in a dot,
    // so that an IRI at the end of a sentence keeps the sentence's full stop.
    private const string LocalName = @"(?<local>[A-Za-z0-9_](?:[A-Za-z0-9_.\-]*[A-Za-z0-9_\-])?)(?![A-Za-z0-9_/#])";

    /// <summary>
    /// Namespace to prefix, longest namespace first. A namespace bound to several prefixes uses the
    /// first declared. A prefix bound to several namespaces is not used: it would print ambiguously.
    /// </summary>
    private static List<KeyValuePair<string, string>> ShorteningTable(IReadOnlyList<KeyValuePair<string, string>> prefixes)
    {
        HashSet<string> ambiguous = [.. prefixes.GroupBy(p => p.Key).Where(g => g.Select(p => p.Value).Distinct().Count() > 1).Select(g => g.Key)];
        Dictionary<string, string> byNamespace = [];
        foreach ((string prefix, string ns) in prefixes)
        {
            if (!ambiguous.Contains(prefix) && ns.Length > 0)
            {
                byNamespace.TryAdd(ns, prefix);
            }
        }

        return [.. byNamespace.OrderByDescending(p => p.Key.Length).ThenBy(p => p.Key, StringComparer.Ordinal)];
    }
}

/// <summary>An RDF term as the model reader sees it.</summary>
internal readonly record struct Term(RdfTermKind Kind, string Value, string? Datatype)
{
    public static Term Iri(string iri) => new(RdfTermKind.Iri, iri, null);

    public bool IsIri => Kind == RdfTermKind.Iri;

    public static Term From(RdfTerm term) => term.Kind switch
    {
        RdfTermKind.Literal => new(term.Kind, Encoding.UTF8.GetString(term.Lexical), Encoding.UTF8.GetString(term.DatatypeIri)),
        RdfTermKind.Iri or RdfTermKind.BlankNode => new(term.Kind, Encoding.UTF8.GetString(term.Lexical), null),
        _ => new(term.Kind, ActMapValidator.Render(term), null),
    };

    public override string ToString() => Kind == RdfTermKind.BlankNode ? "_:" + Value : Value;
}

/// <summary>The IRIs the model reader looks for.</summary>
internal static class Iris
{
    public const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    public const string Rdfs = "http://www.w3.org/2000/01/rdf-schema#";
    public const string Xsd = "http://www.w3.org/2001/XMLSchema#";
    public const string Deed = "https://w3id.org/deed/ns#";
    public const string Cli = "https://w3id.org/deed/cli#";

    public const string Type = Rdf + "type";
    public const string First = Rdf + "first";
    public const string Rest = Rdf + "rest";
    public const string Nil = Rdf + "nil";
    public const string Comment = Rdfs + "comment";
    public const string Label = Rdfs + "label";
    public const string Boolean = Xsd + "boolean";
}
