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
        return data is null ? null : JsonSerializer.Deserialize<object?[]?>(data);
    }
}
