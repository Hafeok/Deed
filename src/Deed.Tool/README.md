# Deed.Tool

The `deed` command. In this preview it does two things: validate an application's
act map against Deed's shapes and print the report as design guidance, and write
the model file that shows exactly what would be generated from the map.

```bash
dotnet tool install --global Deed.Tool --prerelease
```

```bash
deed validate maps/my-tool/my-acts.ttl
```

```bash
deed model maps/my-tool/my-acts.ttl --output my-tool.model.json
```

The data graph is Deed's act vocabulary, its CLI vocabulary and the maps given.
The shapes are Deed's, carried inside the tool. Each result is printed under the
node it is about, with its severity and the shape's message in full: the message
names the design question the map has not yet answered. Counts by severity follow.
IRIs are printed short where the map declares a prefix for them.

`deed model` validates the same way, then derives each act's command, operands,
options, flags, exit codes and read-only, destructive and idempotent hints, and writes
them as versioned JSON to standard output or to `--output`. The format is described in
[docs/model.md](https://github.com/Hafeok/Deed/blob/main/docs/model.md). A map with a
violation, or one the derivation rules refuse, gets no model.

| Exit | Meaning |
|---|---|
| 0 | No violation. Warnings and infos do not fail. For `model`, the model was written. |
| 1 | At least one violation, or (for `model`) maps the derivation rules refuse. |
| 2 | Usage error, a file that cannot be read or does not parse (the parser's line and column are printed), or a SHACL engine that refuses to run the shapes. |

Validation uses [Varve](https://github.com/Hafeok/Varve)'s `Varve.Turtle`,
`Varve.Shacl` and `Varve.Shacl.Sparql`. Nothing the tool validates depends on them.

Source, vocabulary and decisions: <https://github.com/Hafeok/Deed>.
