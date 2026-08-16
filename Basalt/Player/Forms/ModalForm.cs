namespace Basalt.Core.Forms;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

public sealed class ModalForm : Form<object?[]?> {
    private readonly List<object> _content = [];
    public string Submit;

    public ModalForm(string title, string submit = "Submit") : base(title) {
        Submit = submit;
    }

    public ModalForm Dropdown(string text, IReadOnlyList<string> options, int defaultIndex = 0) {
        _content.Add(new {
            type = "dropdown",
            text,
            options,
            @default = defaultIndex
        });
        return this;
    }

    public ModalForm Input(string text, string placeholder = "", string defaultText = "") {
        _content.Add(new {
            type = "input",
            text,
            placeholder,
            @default = defaultText
        });
        return this;
    }

    public ModalForm Label(string text) {
        _content.Add(new {
            type = "label",
            text
        });
        return this;
    }

    public ModalForm Slider(string text, float min, float max, float step = 1, float defaultValue = 0) {
        _content.Add(new {
            type = "slider",
            text,
            min,
            max,
            step,
            @default = defaultValue
        });
        return this;
    }

    public ModalForm StepSlider(string text, IReadOnlyList<string> steps, int defaultIndex = 0) {
        _content.Add(new {
            type = "step_slider",
            text,
            steps,
            @default = defaultIndex
        });
        return this;
    }

    public ModalForm Toggle(string text, bool defaultValue = false) {
        _content.Add(new {
            type = "toggle",
            text,
            @default = defaultValue
        });
        return this;
    }

    public void ClearElements() {
        _content.Clear();
    }

    protected override object CreatePayload() {
        return new {
            type = "custom_form",
            title = Title,
            content = _content,
            submit = Submit
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Deserializing simple types for form responses.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Deserializing simple types for form responses.")]
    protected override object?[]? ReadResponse(string? data) {
        if (data is null) {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(data);
        if (document.RootElement.ValueKind != JsonValueKind.Array) {
            return null;
        }

        JsonElement.ArrayEnumerator values = document.RootElement.EnumerateArray();
        List<object?> response = [];
        foreach (JsonElement value in values) {
            response.Add(ReadValue(value));
        }

        return [.. response];
    }

    private static object? ReadValue(JsonElement value) {
        return value.ValueKind switch {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt32(out int integer) => integer,
            JsonValueKind.Number when value.TryGetInt64(out long longInteger) => longInteger,
            JsonValueKind.Number when value.TryGetDouble(out double number) => number,
            JsonValueKind.Array => value.EnumerateArray().Select(ReadValue).ToArray(),
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ReadValue(property.Value),
                StringComparer.Ordinal),
            _ => null
        };
    }
}
