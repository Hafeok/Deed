# 0038. The model defines each term once, and keeps CLI terms under one key

**Status:** Accepted by Emil, 2026-11-16. Closes issue #45. Refines ADR 0012.

**Context.** The first model format (pull request #44) writes each act's contexts, with their settings and failures, inside every act. The golden files are about 280 KB for Varve and 200 KB for product-cli. A failure's exit code and a term's CLI name sit beside act-layer facts, so a reader that is not a command line cannot tell what to ignore.

**Decision.** The model writes resources, verbs, actors, parameters, settings, contexts, states and failures once each at the top level, keyed by IRI. Acts and hosts refer to them by IRI. Every term from the CLI binding layer is under a `cli` object on the thing it binds: a name override, a short name, an environment variable, a binding override, a file alternative, an exit code, a derived command. The format has not been published, so its version stays 1.

**Consequences.**
- The model says what the vocabulary says: these things are defined once for an application (ADR 0008, ADR 0032).
- The generator gets one type per context, one definition per parameter and one per failure without de-duplicating.
- MCP and REST generation skip every `cli` key and lose nothing else (ADR 0028).
- An act is no longer readable alone; a reader follows references.
- A failure keeps its exit code whether or not the acts that meet it have a command: the code belongs to the failure.

**Alternatives considered.**
- Keep acts self-contained. Rejected: the generator would undo it, and a change shows once per act in a diff.
- Two files, one per layer. Rejected: one more contract for no reader that wants only one.
