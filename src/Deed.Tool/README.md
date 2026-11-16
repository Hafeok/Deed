# Deed.Tool

The `deed` command. In this preview it does one thing: validate an application's
act map against Deed's shapes, and print the report as design guidance.

```bash
dotnet tool install --global Deed.Tool --prerelease
```

```bash
deed validate maps/my-tool/my-acts.ttl
```

The data graph is Deed's act vocabulary, its CLI vocabulary and the maps given.
The shapes are Deed's, carried inside the tool. Each result is printed under the
node it is about, with its severity and the shape's message in full: the message
names the design question the map has not yet answered. Counts by severity follow.

| Exit | Meaning |
|---|---|
| 0 | No violation. Warnings and infos do not fail. |
| 1 | At least one violation. |
| 2 | Usage error, or a file that cannot be read or does not parse (the parser's line and column are printed). |
| 3 | The SHACL engine refused to run the shapes. This is a fault in Deed or the engine, not in the map. |

Validation uses [Varve](https://github.com/Hafeok/Varve)'s `Varve.Turtle`,
`Varve.Shacl` and `Varve.Shacl.Sparql`. Nothing the tool validates depends on them.

Source, vocabulary and decisions: <https://github.com/Hafeok/Deed>.
