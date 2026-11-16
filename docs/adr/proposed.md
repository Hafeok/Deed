# Deed: proposed ADRs from the first vocabulary draft

2026-11-15. All four are **Proposed**. None is accepted until Emil says so. P1 was accepted on 2026-11-15 and is now `0001-a-change-states-its-manner.md`. Evidence and finding numbers refer to `docs/findings/2026-11-15-draft-1-varve.md`. Tracked as issues #13 (P2), #14 (P3), #15 (P4), #16 (P5). They are in one file for review and split into numbered files on acceptance.

Not proposed here, because the evidence does not settle them: namespace, ordering and elision, the verb list, the actor set (findings section 5). The target binding and hosts were decided on 2026-11-15: `0002`, `0003`.

---

## P2. A context is made of settings and owns the failures of everything that uses it

**Status:** Proposed.

**Context.** The facet was described as "target, credentials". In Varve a credential is seven values, and there are two different sets. `Cli.GuardAsync` handles five exception types for every command alike (F3).

**Decision.** `deed:Context` has one or more `deed:Setting` and a `deed:failure` facet of its own. A setting has a name and a datatype and no role. An act names its contexts; it does not repeat their failures. An act's own failure facet may then be `deed:Nothing`, except that an act that changes state must name at least one failure of its own.

**Consequences.**
- `info` has no failure of its own, truthfully.
- A setting and a parameter cannot share a name (shape `NameMeansOneThing`).
- The same context composes across acts: Varve's management credential is used by five acts and one host.
- Nothing yet says that a context may itself write state (F7).

**Alternatives considered.**
- Settings as parameters with a third role. Rejected: a role says how the actor supplies it, and the actor does not.
- Failures only on acts. Rejected: 14 acts would repeat the same six.

---

## P3. Content is a document in the act layer; paths belong to the CLI

**Status:** Proposed.

**Context.** Seven Varve inputs are file paths. A path is meaningless to a remote MCP server and to a REST client. `--query` and `--file` are two spellings of one input (F4).

**Decision.** `deed:Document` is a value kind beside the XSD datatypes. A parameter whose value is the content of a file says `deed:Document`. The CLI binds a document as a path and reads it. A text parameter may declare `cli:fileAlternative`, the option that supplies it from a file; exactly one of the two is given, and the act layer sees one parameter.

**Consequences.**
- The "inline or file, not both" check becomes a parse-time usage error rather than a hand-written failure.
- MCP and REST bindings must each say how a document arrives. Not designed yet.
- A document's media type is not modelled. `load` infers syntax from the extension, which only a path has.

**Alternatives considered.**
- A path datatype. Rejected: it lets one surface's syntax into the act layer.
- Two parameters, text and file, with an exclusion constraint. Rejected: it models a spelling, and needs a constraint term nothing else requires.

---

## P4. One command is one act; a flag never selects the act

**Status:** Proposed.

**Context.** Nine of Varve's 22 acts are reached through a flag of another command or a path that is not resource path plus verb. `apply` is four acts; `lease` is three. Their facets differ, so they cannot be one act with conditional facets, and the vocabulary has no conditional facets by design (F6).

**Decision.** The generator emits exactly one command per act, at the derived path. `cli:spelledAs` exists to record a current spelling that cannot be derived. Validation reports it as a warning and the generator refuses to emit it.

**Consequences.**
- Adopting Deed in Varve changes nine spellings, or keeps those commands hand-written until it does. For example `lease break`, `lease takeover`, `declaration plan`.
- Required-only-with-another-flag disappears: `--epoch` is simply required on `lease takeover`.
- The warning count is a migration measure: Varve starts at 10.

**Alternatives considered.**
- A term that binds a boolean flag of one command to another act. Rejected: it reproduces the conditional facets on the surface and makes help text and exit codes ambiguous.
- Aliases, so the old spelling stays valid. Not rejected; deferred until a migration needs it.

---

## P5. Options that exist only because the surface is a command line are declared in the CLI layer

**Status:** Proposed.

**Context.** `--format` is content negotiation. `--out` saves an outcome that another surface would return. `--exit-code` signals an outcome that another surface would read. None corresponds to anything in the act (F5, F8).

**Decision.** `cli:SurfaceOption` declares such an option, and `cli:surfaceOption` attaches it to an act's command. The act layer does not mention it. A shape asks of each: why does only the command line need this?

**Consequences.**
- MCP and REST never see these options.
- Failures that arise only from a surface option (an unsupported format) are not act failures. Where they are declared is open.
- It is an escape hatch. The shape's question is the only guard against moving real parameters into it.

**Alternatives considered.**
- Format as a modifier. Rejected: it would appear in MCP tool schemas and REST query strings beside `Accept`.
- A dedicated representation facet on the outcome. Premature: two Varve acts need it.
