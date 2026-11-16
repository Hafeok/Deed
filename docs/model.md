# The model file

`deed model <map.ttl>...` validates act maps, applies the derivation rules and writes
the result as JSON: the model file of ADR 0012. It is everything the source generator
needs and nothing it must derive. This page describes version 1. The JSON Schema is
[`model.schema.json`](model.schema.json), beside this page.

```bash
deed model maps/varve/varve-acts.ttl --output varve.model.json
```

Golden files for the two act maps in this repository are
[`tests/fixtures/varve.model.json`](../tests/fixtures/varve.model.json) and
[`tests/fixtures/product-cli.model.json`](../tests/fixtures/product-cli.model.json).

## Guarantees

- **Versioned.** The top-level `deedModel` is the format's version, `1`. A change that
  a reader of version 1 could misread raises it.
- **Deterministic.** The same maps give the same bytes: UTF-8, two-space indent, `\n`
  line ends, a final newline, properties in the order this page lists them. Lists that
  RDF orders (an act's input) keep that order. Lists RDF leaves unordered (actors,
  contexts, settings, needs, failures, hosts' acts) are sorted by IRI, ordinally.
  Options and flags are sorted by long name.
- **Complete.** Every property is always written. A value that is absent is `null`
  (a comment, a CLI name, a short name) or `[]` (a list).
- **Full IRIs.** The model is data: IRIs are never shortened (ADR 0022).
- **Written only when sound.** `deed model` writes nothing, and exits 1, when the maps
  have a violation or a derivation rule refuses them. Both are reported on standard
  error, with IRIs shortened by the maps' prefixes.

Names are camelCase. Enumerated values are lower case: `subject`, `modifier`,
`creates`, `appends`, `converges`, `removes`, `single`, `stream`, `parameter`,
`setting`, `surfaceOption`, `fileAlternative`.

## Top level

| Property | Value |
|---|---|
| `deedModel` | `1` |
| `tool` | The tool: below. |
| `acts` | Every `deed:Act`, sorted by IRI. |
| `hosts` | Every `cli:Host`, sorted by IRI. |

The maps must declare exactly one `cli:Tool`. Zero or several is a derivation error.

### `tool`

| Property | Value |
|---|---|
| `iri` | The `cli:Tool`. |
| `command` | `cli:command`: the root command. |
| `defaultResource` | The `cli:defaultResource` as a *named term*, or `null`. |
| `defaultVerb` | The `cli:defaultVerb` as a *named term*, or `null`. |

A **named term** (a resource or a verb) is `{ iri, name, cliName, comment }`: its
`deed:name`, its `cli:name` or `null`, and its `rdfs:comment` or `null`.

## An act

The act layer first, resolved; then what is derived from it.

| Property | Value |
|---|---|
| `iri` | The act. |
| `comment` | Its `rdfs:comment`. |
| `resourcePath` | Named terms: the act's resource and every resource it is `deed:within`, outermost first. |
| `verb` | A named term. |
| `actors` | `{ iri, label, comment }` per `deed:actor`. |
| `input` | The parameters in declared order: `{ iri, name, role, datatype, required, repeatable, comment }`. `[]` for `deed:Nothing`. |
| `contexts` | Per `deed:context`: `{ iri, comment, settings, failures }`. `settings` are `{ iri, name, datatype, repeatable, comment }`; `failures` are the IRIs of the context's own failures. `[]` for `deed:Nothing`. |
| `needs` | `{ iri, comment }` per state read. |
| `changes` | `{ state, manner }` per change. |
| `outcome` | `{ kind, yields }`: `single` or `stream`, and the IRI of what it yields. Outcome kinds are IRIs only; describing them is a later slice (ADR 0013). |
| `failures` | Every failure the act can meet, its own and its contexts', once each: `{ iri, name, comment, exitCode, own, contexts }`. `own` is true when the act declares it; `contexts` lists the act's contexts that carry it. |
| `hints` | `{ readOnly, destructive, idempotent }`. |
| `cli` | `{ command, spelledAs }`: one of the two is `null`. |

Datatypes are IRIs: an XSD datatype, or `https://w3id.org/deed/ns#Document` for content
supplied whole.

### `hints` (ADR 0001, 0006)

- `readOnly`: the act changes `deed:Nothing`.
- `destructive`: any change converges or removes.
- `idempotent`: read-only, or every change converges or removes.

### `exitCode` (ADR 0020)

`cli:exitCode` where the failure assigns one, otherwise `1`. Exit 0 is the act's
outcome and 2 a usage error; neither is a failure's.

### `cli`

An act with `cli:spelledAs` has `command: null` and `spelledAs` holding the recorded
spelling: no command is derived for it (ADR 0010). It keeps every facet and its hints,
which MCP and REST bind to. Any other act has `spelledAs: null` and a derived
`command`:

| Property | Value |
|---|---|
| `line` | The tool's command and the path, joined by single spaces: `varve storage readers forget`. |
| `path` | The tokens after the tool's command. |
| `operands` | In position order: `{ name, datatype, required, repeatable, comment, source }`. |
| `options` | Options that take a value: `{ long, short, env, datatype, required, repeatable, alternativeTo, comment, source }`. |
| `flags` | Options that take none: `{ long, short, env, comment, source }`. |

`source` is `{ kind, iri }`: the term the element is bound to, so a generator can
carry its value to the act.

**Path** (ADR 0007). The resource path outermost first, without the tool's default
resource, then the verb unless it is the tool's default verb. Each token is the term's
`cli:name` where it has one, else its `deed:name`. Two acts deriving the same `line`
is a derivation error naming both.

**Operands** (ADR 0002, 0018). First the setting bound as an operand, if one of the
act's contexts has it: `required` is always true, since it has no environment fallback.
Then the subjects in declared order, except a subject bound `cli:bindAs cli:Option`.
Then the modifiers bound `cli:bindAs cli:Operand`, in declared order. A parameter
operand's `required` and `repeatable` are the parameter's.

**Options and flags.** Every modifier not bound as an operand, every subject bound as
an option, every setting of the act's contexts that is not operand-bound (each once,
however many contexts carry it), and every `cli:surfaceOption`. A boolean datatype
makes a flag. `long` is the `cli:name` or `deed:name`; `short` is `cli:short` and `env`
is `cli:env` where assigned. A parameter option's `required` is the parameter's; a
setting or a CLI-only option is never required on the command line.

A parameter's `cli:fileAlternative` adds one more option: `long` is the alternative's
name, `datatype` is `deed:Document`, `alternativeTo` is the long name of the
parameter's own option, and `source` is `{ "kind": "fileAlternative", "iri": <the
parameter> }`. Exactly one of the two is given; the parameter's `required` means one of
the two is required.

Two terms deriving the same long name, or the same short name, in one command is a
derivation error naming both.

## A host

`{ iri, name, comment, contexts, performs, spelledAs }`: the host's `deed:name`, its
contexts in the act's form, the IRIs of the acts it performs, and its `cli:spelledAs`
or `null`. No command line is derived for a host in version 1.

## Derivation errors

Each names the terms it is about. `deed model` reports all of them, not only the first.

- The maps declare no `cli:Tool`, or more than one.
- Two acts derive the same command (ADR 0007).
- A second setting is bound as an operand (ADR 0018).
- An operand-bound setting has `cli:env` (ADR 0018).
- Two terms derive the same long or short option in one command.
