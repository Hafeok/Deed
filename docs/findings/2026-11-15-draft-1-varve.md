# Deed vocabulary, draft 1: findings from modelling Varve

2026-11-15. Source: Hafeok/Varve at `e684009` (2026-11-12), `src/Varve.Server/Commands`,
ADRs 0037, 0105, 0127, 0130, 0137, 0163, 0181.

Files: `vocabulary/deed.ttl`, `vocabulary/deed-cli.ttl`, `vocabulary/deed-shapes.ttl`, `maps/varve/varve-acts.ttl`, `docs/adr/proposed.md`.

As written, nothing here moved an item from Open to Decided. **Decided since, by Emil on 2026-11-15:** manner on changes (F2, `docs/adr/0001`); the target stays context with a declared operand binding (F1, `docs/adr/0002`); hosts are a CLI-layer kind (Q5, `docs/adr/0003`). Q1 to Q4 and proposed ADRs P2 to P5 remain open, as issues #4 to #7 and #13 to #16. Where this note says "the brief" or "the project instructions" it means the starting decisions listed in issue #2.

## 1. Result

Varve's 18 registered commands are 22 acts and 2 host commands. All 22 acts state all seven facets and validate with no violations.

| How the current spelling is reached | Acts | Which |
|---|---|---|
| Derived by rule alone | 1 | `create` |
| Derived with a declared override | 9 | `info`, `query`, `update`, `export`, `checkpoint`, `feed`, `storage`, `storage readers list`, `storage readers forget` |
| Near miss | 3 | `load`, `setup apply`, `lease` (show) |
| Not derivable | 9 | `lease --break`, `lease --take`, `apply`, `apply --plan`, `apply --from-plan`, `apply --from-config`, `manage move-events`, `manage takeover`, `serve --print-config` |

The act layer held up: no Varve command needed a facet to be optional or conditional once it was split into the acts it actually is. The CLI derivation did not hold up as stated: one rule (context becomes a global option) contradicts how Varve spells every data command, and nine acts are reached through a flag or a path the rules cannot produce.

One reference fact in the project instructions is wrong. Varve has no `lease take`; it is `lease <dataset> --take --epoch <n>`. Its noun-verb commands are `setup apply` and `storage readers list|forget`.

## 2. What modelled cleanly, and what did not

### Clean

- **`info`**: no input, three states read, single outcome, no failure of its own. Everything that can go wrong comes from its context.
- **`query`**: one subject, two modifiers, a stream. Target-bound through context.
- **`create`, `update`, `checkpoint`, `export`, `feed`**: each one resource, one verb, one outcome.
- **`setup apply`**: multi-step, and still one act. It converges three states and applying again commits nothing, so the steps are detail of the outcome, not acts.
- **`storage readers list|forget`**: resource path plus verb, exactly as the rule says.
- **Bulk streams**: `export` and `feed` are stream outcomes; `load` takes repeatable documents. No new term beyond `deed:Stream` and `deed:repeatable`.
- **Failures**: `Cli.GuardAsync` catches five exception types for every command. They became failures of the context, stated once, and no act repeats them.

### Not clean: findings about the vocabulary

**F1. The target cannot be both context and the first operand.** The brief's rule makes context a global option with an environment fallback. Varve spells the dataset as an operand of 12 commands, first in 11 of them. Nine acts derive only with `cli:bindAs cli:Operand` on the `dataset` setting. The management commands already follow the rule (`--server`), though with no environment fallback. Consequence of operand-as-target: the real subject is pushed into an option (`--query`, `--update`, `--reader`), which is three more overrides. The role assignment in the act layer is right; the CLI rule or Varve's spelling has to give. See Q2.

**F2. Effect cannot be derived from Needs and Changes as stated (decision 5).** "No Changes means read-only" works. Destructive and idempotent cannot be told from *which* state is written; they depend on *how*. `update` and `storage readers forget` both write state; one appends to history, the other removes a record. I added `deed:manner` on each change with four values, each forced by a Varve act: `Creates` (`create`, `checkpoint`), `Appends` (`update`, `load`), `Converges` (`apply`, `setup apply`, `move-events`, both takeovers), `Removes` (`lease --break`, `readers forget`). This amends decision 5 and needs your call (ADR P1).

**F3. A context has parts and failures.** "Credentials" in Varve is seven options, and two different sets of seven for the data plane and the management plane. Context became a class with `deed:setting` and `deed:failure`. Settings are not parameters: they have no role.

**F4. File paths are surface syntax.** `load <files>`, `setup apply <setup.yaml>`, `apply <files>`, `--from-plan`, `--from-config` and `--file` all name paths. A path means nothing to a remote MCP server or a REST client. I added `deed:Document` as a value kind and `cli:fileAlternative` for text given inline or by file. `--query` and `--file` are one parameter in the act layer.

**F5. `--format` is surface syntax.** It is content negotiation: REST has `Accept`, MCP returns structure. It is not in the act layer. It forced `cli:SurfaceOption`, which also holds `apply`'s `--out` and `--exit-code`. Their format failures (a local graph result is N-Triples only) have no home in the act layer and are not modelled.

**F6. Facets cannot be conditional, and that is correct.** `apply` reaches a server or not, changes state or not, depending on `--plan`, `--from-plan` and `--from-config`. `lease` reads, removes or converges depending on `--break` and `--take`. Neither can be one act. I treated this as a finding about the commands: the vocabulary is defensible, and the shapes ask the right question ("required only when another parameter is given: two acts?").

**F7. A read-only act whose context writes.** A read of an object-store dataset writes a reader record and deletes it on exit (ADR 0160); a remote command writes a refresh token to the credential file. Neither is in Changes, so `query` derives read-only. The reader record is real shared state: a crashed read leaves one that needs `forget`. Not modelled; the vocabulary has no way to say a context writes. Open.

**F8. Outcome signalled by exit code.** `lease` exits 1 when the lock is held, having printed a full answer. `apply --plan --exit-code` exits 2 on drift. Neither is a failure in the act-layer sense: the actor has the outcome. `lock-held` is modelled as a failure of `lease` because that is what the code does; on MCP that would be `isError` on a successful read, which is wrong. Drift is left as a CLI-only option.

**F9. Roles depend on what the act finds.** `setup apply` needs operator to create a missing dataset and less when it exists. Actor lists every role that may be needed; scopes derived from Changes give the maximum. The vocabulary cannot say "operator only if the dataset is missing". Not addressed.

### Not clean: findings about the commands

Recorded for Varve, not acted on:

- `--server` means one address (`apply`, `move-events`) and an ordered list of candidates (`takeover`).
- `--from` means a position (`feed`) and a failed instance (`takeover`).
- `--graph` is a destination in `load` and a filter in `export` and `feed`. Modelled as one parameter; the shape cannot detect the drift.
- `setup apply <setup> <dataset>` puts the dataset last; every other command puts it first.
- `load` registers `--allow-endpoint` and nothing else of the outbound policy.
- Input validation exits 1, not 2: `--format`, `--entailment`, `--to` with `--follow`, `--query` with `--file`. A generator would reject these at parse time with 2.
- `lease --take` and `manage takeover` are the same idea under two verbs.
- `manage` is not a resource. It groups by plane, and inside it the commands are verb-noun compounds (`move-events`, `watch-kubernetes`).
- Capability differs by where the dataset is: `--entailment` needs a server, `export --as-of` needs a local dataset. Modelled as failures `needs-server` and `needs-local`.

## 3. Derivation rules, as tested

| # | Rule | Tested against Varve |
|---|---|---|
| R1 | Command path = resource path (outermost first) + verb. | Holds for 3 of 22 as written (`setup apply`, `storage readers list`, `storage readers forget`). With R1a and R1b: 13 of 22. |
| R1a | The tool's one default resource is left out of the path. | Needed by 6: `create`, `load`, `query`, `update`, `export`, `checkpoint`. Not in the brief as a rule; pending Q2. |
| R1b | The tool's one default verb is left out of the path. | Needed by 4: `info`, `feed`, `lease`, `storage`. Not in the brief; pending Q2. |
| R2 | Subjects become operands in declared order. | Holds for `create`, `load`, `setup apply`, `apply`, `move-events`, `takeover`. Fails for `query`, `update`, `readers forget`, where the subject is an option because the dataset holds the operand position (F1). |
| R3 | Modifiers become long options; boolean modifiers become flags. | Holds for all 14 modifiers. One (`--from` on `takeover`) needs a name override because `feed` already owns `from`. |
| R4 | Short names only when assigned. | Holds: `-q`, `-u`, `-f` are the only three. |
| R5 | Context becomes a global option with an environment fallback. | Credentials: holds (`--token`/`VARVE_TOKEN`, `--client-secret`/`VARVE_CLIENT_SECRET`); the other settings have no fallback, so the variable is assigned, not derived. Target: fails for the dataset (operand, 12 commands), holds for `--server`. Options are per command in Varve, not global. |
| R6 | On MCP, context is bound at server start and hidden from the schema. | Not testable: Varve has no MCP surface. Consistent with the model: no act takes a setting as input. |
| R7 | No Changes means read-only. | Holds for all 10 read acts, subject to F7. |
| R8 | Destructive and idempotent follow from the manners: destructive if any change converges or removes; idempotent if every change converges or removes. | Agrees with the code's own statements for `setup apply`, `move-events`, `takeover` ("applying it again commits nothing", "idempotent", "a late trigger is a no-op"). Two doubtful: `update` derives non-destructive, which is true of the log and false of the current graph; `checkpoint` derives non-idempotent and is probably harmless to repeat. |
| R9 | HTTP method: none → GET, creates/appends → POST, converges → PUT, removes → DELETE. | Matches `status`, `feed`, `update`, `checkpoint`. Fails for the three converging acts Varve exposes: `/manage/apply`, `events/move` and takeover are all POST. `query` needs POST for long text. PUT fits only when the URL names what is replaced. Rule not ready. |
| R10 | A failure exits 1; `isError` with the failure's name on MCP; a status with problem details on REST. | CLI: holds for 38 of 40 failures. Two exit 2 and carry `cli:exitCode`: `setup-outside-subset`, `configuration-invalid`. See F8 for the two cases that are not failures. MCP and REST: not testable. No status-code mapping was attempted; no term for it exists. |

## 4. Vocabulary terms

### Added or changed because of Varve

Act layer, beyond the brief's list:

| Term | Forced by |
|---|---|
| `deed:within` | `storage readers` |
| `deed:State` | `checkpoint` needs admin, `update` needs write, on the same dataset |
| `deed:Change`, `deed:state`, `deed:manner`, four manners | F2 |
| `deed:Setting`, `deed:setting` | F3 |
| `deed:failure` on a context | `Cli.GuardAsync` |
| `deed:Document` | F4 |
| `deed:required`, `deed:repeatable` | `--reader`, `--oldest`, `--newest` are required options; `<files...>` |
| `deed:name` | one token per term for every surface |

CLI layer. From the brief: root command (`cli:Tool`, `cli:command`), short name, environment fallback, exit code. Command path, operand, option and flag are derived and have no term. Added:

| Term | Forced by |
|---|---|
| `cli:defaultResource`, `cli:defaultVerb` | R1a, R1b |
| `cli:name` | `info`, `readers`, `--from`, `--server` |
| `cli:bindAs` | F1 |
| `cli:fileAlternative` | `--query` or `--file` |
| `cli:SurfaceOption`, `cli:surfaceOption` | F5 |
| `cli:spelledAs` | the nine underivable acts; record only, a generator refuses it |
| `cli:Host`, `cli:performs` | `serve`, `watch-kubernetes`; pending Q5 |

### Tempted, not added

- An event class for Changes. Every Varve write is modelled as a state change.
- `deed:Nothing` as an outcome. Every Varve act returns something.
- Target and credential as kinds of context. No rule distinguishes them yet.
- A link from state to resource, for deriving scope names.
- Constraints between parameters: enumerations, mutual exclusion, requires. Modelled as failures instead.
- Default values (`--memory` 256, `--from` 0). They are in the comment.
- A witness role for parameters. `--if-match`, `--epoch`, `--oldest`, `--newest`, `--from` and `--from-plan` all make the actor restate what they saw. It is Varve's substitute for confirmation and deserves a name later.
- Progress. `load` prints a line per file before its outcome.
- An unbounded marker for streams (`feed --follow`).
- A mode or capability term for embedded versus remote.
- A secret marker on settings. No tested rule reads it.
- An order among actors (reader < writer < admin < operator).
- A way for a context to declare what it writes (F7).
- CLI: per-resource elision, aliases (bare `varve` runs the server), pass-through tokens (`--Varve:Key=Value`), short-name uniqueness per command.

## 5. Recommendations on the open questions

Yours to decide.

**Q1. Namespace IRI and prefix.** Recommend `https://w3id.org/deed/ns#` with prefix `deed:`, `https://w3id.org/deed/cli#` with `cli:`, shapes at `https://w3id.org/deed/shapes#`. Evidence: Varve's own vocabulary is at `https://w3id.org/varve/ns#`, so the same registrar and the same hash style. Cost: a pull request to w3id.org. A tool's acts live in the tool's namespace; the `varve:` IRI in `varve-acts.ttl` is a placeholder.

**Q2. Ordering and elision.** Recommend resource-then-verb, with one default resource and one default verb per tool.
- Ordering: every Varve command that has both is resource first (`setup apply`, `storage readers list`, `storage readers forget`). The two verb-first compounds sit under `manage`, which is not a resource.
- Default resource: six bare verbs need it. Without it they become `dataset create` and so on.
- Default verb (`show`): four bare nouns need it (`info`, `feed`, `lease`, `storage`). This was not in the brief. It makes `info` a resource named `status` whose verb is unsaid, which matches `GET /status`.
- With both, 13 of 22 paths derive. Risk: a default-resource verb and a default-verb resource can collide on one token. A path-uniqueness shape is needed and not yet written.
- Separate decision inside this one: whether a target binds as the leading operand (F1). Recommend allowing it as a declared binding on one setting per tool, since Varve does it in 12 commands and it reads well. The cost is R2: subjects of target-bound acts then compete for position.

**Q3. Controlled verb list.** Varve uses 15: `create`, `show` (unsaid), `list`, `load`, `query`, `update`, `export`, `checkpoint`, `plan`, `apply`, `convert`, `move`, `takeover`, `break`, `forget`. Recommend Deed ships the four that are not about RDF or storage (`create`, `show`, `list`, `apply`) and a tool adds its own, each defined once and validated by the same shape. Decide with it: whether `plan` is a verb or a mode of `apply`; whether `takeover` and `lease --take` are one verb (modelled as one); whether a `remove` belongs in the core (no Varve command needs it; `forget` is deliberately not a delete).

**Q4. Initial actor set.** Recommend Deed ships none: the class and its shape only. Varve's five are in `varve-acts.ttl`: `reader`, `writer`, `dataset admin`, `operator` from ADRs 0037 and 0163, and `custodian` for whoever holds the storage, which no grant covers (ADR 0105 embedded mode, ADR 0137).
- On-behalf-of: recommend no term. ADR 0127 point 4: the agent stays the subject and the server cannot tell an exchanged token from any other. The actor is the role the subject holds.
- Confirmation: recommend no term. Varve never prompts. It asks for a witness instead (see the tempted list). A shape already steers a required boolean toward that.
- Automation holds `operator` like a person does (ADR 0181 point 3), which supports "a role, never an identity".

**Q5. Long-running processes.** Recommend a separate kind in the CLI binding layer only, outside the act layer. Evidence:
- `serve` and `watch-kubernetes` have no outcome and no enumerable failures, and neither can be an MCP tool or a REST route.
- The generated MCP server (decision 10) is the same kind of command, so the binding layer needs the notion anyway.
- Each host hides an act: `serve --print-config` (modelled as `configuration show`), `serve --record-bootstrap` (a system commit at start, not modelled), and `watch-kubernetes`, which performs `writer takeover` on each event.
- `feed --follow` is not a host. It is an act with a stream that does not end.

**Stream bounds on MCP** (not asked; one data point). `feed` already pages by position with `--from` and `--to`. `query` and `export` have no cursor. That suggests pagination where the stream has a natural cursor and a cap elsewhere.

## 6. Validation report

Validator: pySHACL 0.40.2 over rdflib 7.6.0, SHACL-SPARQL enabled, no inference. **Not Varve.Shacl**: no .NET SDK was available in this session. Decision 11 requires the Varve.Shacl run before these shapes are trusted. They use sequence paths, `sh:zeroOrMorePath`, `sh:or`, `sh:not` and three SPARQL constraints, so `Varve.Shacl.Sparql` is needed too.

Data graph: `deed.ttl` + `deed-cli.ttl` + `varve-acts.ttl`. Counts: 22 acts, 12 resources, 15 verbs, 5 actors, 25 parameters, 34 settings, 11 contexts, 13 states, 40 failures.

| Severity | Count | What |
|---|---|---|
| Violation | 0 | |
| Warning | 10 | `cli:spelledAs` on 9 acts and on the `watch-kubernetes` host |
| Info | 8 | `cli:bindAs` on `dataset`, `query`, `update`, `reader`; `cli:name` on `status`, `reader`, `failed-instance`, `standby` |

Warnings: `ApplyDeclarations`, `ApplyPlan`, `BreakLease`, `ConvertConfiguration`, `MoveEventDeclarations`, `PlanDeclarations`, `ShowConfiguration`, `TakeOverLease`, `TakeOverWriter`, `WatchKubernetes`. Each reads: "This act's command line is recorded, not derived: no resource path and verb spell it. Nothing can be generated for it. What would it be called as a command of its own?"

A clean pass proves little, so the shapes were also run against a deliberately broken vocabulary (not delivered): 22 violations, including the three SPARQL ones. (Corrected 2026-11-16, #36: this said "every shape firing"; the fixture reaches 17 of the 61 messages. `tests/fixtures/broken-terms.ttl` reaches the rest, and a test now checks that every message is reached by some fixture.) For example: "This act changes https://example.org/x#LeaseState but declares no failure of its own. What happens when https://example.org/x#LeaseState is not as the actor expects: already there, already held, moved on since they looked?" The message prints the full IRI; a label would read better and needs a decision on which label property the shapes may assume.

The derivation table in section 1 and the effect column in R8 and R9 were computed from the Turtle by a throwaway script and compared with a hand-written list of each command's operands and options taken from the source. The script is not delivered.

## 7. Not done

- Outcome kinds (`varve:DatasetStatus` and the rest) are named and not described. Nothing yet says what a status contains.
- No REST or MCP binding vocabulary, and no status-code mapping.
- Manifold, the NuGet and GitHub name check, and the dependency-direction ADR were not touched.
- `serve --record-bootstrap` is listed as a setting of the host and not modelled as the act it is.
