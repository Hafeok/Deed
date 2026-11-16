using System.Text.Encodings.Web;
using System.Text.Json;

namespace Deed.Vocabulary.Model;

/// <summary>
/// Writes a model as the JSON of <c>docs/model.md</c>. Written by hand with <see cref="Utf8JsonWriter"/>:
/// no reflection, so the tool stays trim and AOT compatible (ADR 0014), and every property is written
/// in a fixed order, so the same model always gives the same bytes. Absent values are written as null.
/// </summary>
public static class ModelWriter
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",
        // The file is not embedded in HTML: comments keep their <, >, & and backticks as written.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>The model as UTF-8 JSON, ending in a newline.</summary>
    public static byte[] ToUtf8(DeedModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        using MemoryStream buffer = new();
        using (Utf8JsonWriter w = new(buffer, Options))
        {
            w.WriteStartObject();
            w.WriteNumber("deedModel", DeedModel.Version);
            w.WritePropertyName("tool");
            Tool(w, model.Tool);
            Array(w, "acts", model.Acts, Act);
            Array(w, "hosts", model.Hosts, Host);
            w.WriteEndObject();
        }

        buffer.WriteByte((byte)'\n');
        return buffer.ToArray();
    }

    private static void Tool(Utf8JsonWriter w, ToolModel t)
    {
        w.WriteStartObject();
        w.WriteString("iri", t.Iri);
        w.WriteString("command", t.Command);
        w.WritePropertyName("defaultResource");
        NamedOrNull(w, t.DefaultResource);
        w.WritePropertyName("defaultVerb");
        NamedOrNull(w, t.DefaultVerb);
        w.WriteEndObject();
    }

    private static void Act(Utf8JsonWriter w, ActModel a)
    {
        w.WriteStartObject();
        w.WriteString("iri", a.Iri);
        w.WriteString("comment", a.Comment);
        Array(w, "resourcePath", a.ResourcePath, Named);
        w.WritePropertyName("verb");
        Named(w, a.Verb);
        Array(w, "actors", a.Actors, (w, x) =>
        {
            w.WriteStartObject();
            w.WriteString("iri", x.Iri);
            w.WriteString("label", x.Label);
            w.WriteString("comment", x.Comment);
            w.WriteEndObject();
        });
        Array(w, "input", a.Input, (w, p) =>
        {
            w.WriteStartObject();
            w.WriteString("iri", p.Iri);
            w.WriteString("name", p.Name);
            w.WriteString("role", p.Role == ParameterRole.Subject ? "subject" : "modifier");
            w.WriteString("datatype", p.Datatype);
            w.WriteBoolean("required", p.Required);
            w.WriteBoolean("repeatable", p.Repeatable);
            w.WriteString("comment", p.Comment);
            w.WriteEndObject();
        });
        Array(w, "contexts", a.Contexts, Context);
        Array(w, "needs", a.Needs, (w, s) =>
        {
            w.WriteStartObject();
            w.WriteString("iri", s.Iri);
            w.WriteString("comment", s.Comment);
            w.WriteEndObject();
        });
        Array(w, "changes", a.Changes, (w, c) =>
        {
            w.WriteStartObject();
            w.WriteString("state", c.State);
            w.WriteString("manner", c.Manner.ToString().ToLowerInvariant());
            w.WriteEndObject();
        });
        w.WritePropertyName("outcome");
        w.WriteStartObject();
        w.WriteString("kind", a.Outcome.Kind == OutcomeKind.Stream ? "stream" : "single");
        w.WriteString("yields", a.Outcome.Yields);
        w.WriteEndObject();
        Array(w, "failures", a.Failures, (w, f) =>
        {
            w.WriteStartObject();
            w.WriteString("iri", f.Iri);
            w.WriteString("name", f.Name);
            w.WriteString("comment", f.Comment);
            w.WriteNumber("exitCode", f.ExitCode);
            w.WriteBoolean("own", f.Own);
            Strings(w, "contexts", f.Contexts);
            w.WriteEndObject();
        });
        w.WritePropertyName("hints");
        w.WriteStartObject();
        w.WriteBoolean("readOnly", a.Hints.ReadOnly);
        w.WriteBoolean("destructive", a.Hints.Destructive);
        w.WriteBoolean("idempotent", a.Hints.Idempotent);
        w.WriteEndObject();
        w.WritePropertyName("cli");
        Cli(w, a.Cli);
        w.WriteEndObject();
    }

    private static void Cli(Utf8JsonWriter w, CliModel cli)
    {
        w.WriteStartObject();
        if (cli.Command is not { } c)
        {
            w.WriteNull("command");
            w.WriteString("spelledAs", cli.SpelledAs);
            w.WriteEndObject();
            return;
        }

        w.WritePropertyName("command");
        w.WriteStartObject();
        w.WriteString("line", c.Line);
        Strings(w, "path", c.Path);
        Array(w, "operands", c.Operands, (w, o) =>
        {
            w.WriteStartObject();
            w.WriteString("name", o.Name);
            w.WriteString("datatype", o.Datatype);
            w.WriteBoolean("required", o.Required);
            w.WriteBoolean("repeatable", o.Repeatable);
            w.WriteString("comment", o.Comment);
            Source(w, o.Source);
            w.WriteEndObject();
        });
        Array(w, "options", c.Options, (w, o) =>
        {
            w.WriteStartObject();
            w.WriteString("long", o.Long);
            w.WriteString("short", o.Short);
            w.WriteString("env", o.Env);
            w.WriteString("datatype", o.Datatype);
            w.WriteBoolean("required", o.Required);
            w.WriteBoolean("repeatable", o.Repeatable);
            w.WriteString("alternativeTo", o.AlternativeTo);
            w.WriteString("comment", o.Comment);
            Source(w, o.Source);
            w.WriteEndObject();
        });
        Array(w, "flags", c.Flags, (w, f) =>
        {
            w.WriteStartObject();
            w.WriteString("long", f.Long);
            w.WriteString("short", f.Short);
            w.WriteString("env", f.Env);
            w.WriteString("comment", f.Comment);
            Source(w, f.Source);
            w.WriteEndObject();
        });
        w.WriteEndObject();
        w.WriteNull("spelledAs");
        w.WriteEndObject();
    }

    private static void Source(Utf8JsonWriter w, CliSource s)
    {
        w.WritePropertyName("source");
        w.WriteStartObject();
        w.WriteString("kind", s.Kind switch
        {
            CliSourceKind.Parameter => "parameter",
            CliSourceKind.Setting => "setting",
            CliSourceKind.SurfaceOption => "surfaceOption",
            _ => "fileAlternative",
        });
        w.WriteString("iri", s.Iri);
        w.WriteEndObject();
    }

    private static void Host(Utf8JsonWriter w, HostModel h)
    {
        w.WriteStartObject();
        w.WriteString("iri", h.Iri);
        w.WriteString("name", h.Name);
        w.WriteString("comment", h.Comment);
        Array(w, "contexts", h.Contexts, Context);
        Strings(w, "performs", h.Performs);
        w.WriteString("spelledAs", h.SpelledAs);
        w.WriteEndObject();
    }

    private static void Context(Utf8JsonWriter w, ContextModel c)
    {
        w.WriteStartObject();
        w.WriteString("iri", c.Iri);
        w.WriteString("comment", c.Comment);
        Array(w, "settings", c.Settings, (w, s) =>
        {
            w.WriteStartObject();
            w.WriteString("iri", s.Iri);
            w.WriteString("name", s.Name);
            w.WriteString("datatype", s.Datatype);
            w.WriteBoolean("repeatable", s.Repeatable);
            w.WriteString("comment", s.Comment);
            w.WriteEndObject();
        });
        Strings(w, "failures", c.Failures);
        w.WriteEndObject();
    }

    private static void Named(Utf8JsonWriter w, NamedTerm t)
    {
        w.WriteStartObject();
        w.WriteString("iri", t.Iri);
        w.WriteString("name", t.Name);
        w.WriteString("cliName", t.CliName);
        w.WriteString("comment", t.Comment);
        w.WriteEndObject();
    }

    private static void NamedOrNull(Utf8JsonWriter w, NamedTerm? t)
    {
        if (t is null)
        {
            w.WriteNullValue();
        }
        else
        {
            Named(w, t);
        }
    }

    private static void Strings(Utf8JsonWriter w, string name, IReadOnlyList<string> values)
    {
        w.WriteStartArray(name);
        foreach (string v in values)
        {
            w.WriteStringValue(v);
        }

        w.WriteEndArray();
    }

    private static void Array<T>(Utf8JsonWriter w, string name, IReadOnlyList<T> items, Action<Utf8JsonWriter, T> write)
    {
        w.WriteStartArray(name);
        foreach (T item in items)
        {
            write(w, item);
        }

        w.WriteEndArray();
    }
}
