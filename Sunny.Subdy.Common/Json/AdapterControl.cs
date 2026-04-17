using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Json
{
    public class CheckBoxGetterAdapter : IConfigurableControl
    {
        private readonly CheckBox control;
        public CheckBoxGetterAdapter(CheckBox c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Checked);
    }
    public class UICheckBoxGetterAdapter : IConfigurableControl
    {
        private readonly AntdUI.Checkbox control;
        public UICheckBoxGetterAdapter(AntdUI.Checkbox c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Checked);
    }
    public class TextBoxGetterAdapter : IConfigurableControl
    {
        private readonly TextBox control;
        public TextBoxGetterAdapter(TextBox c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Text);
    }

    public class ComboBoxGetterAdapter : IConfigurableControl
    {
        private readonly ComboBox control;
        public ComboBoxGetterAdapter(ComboBox c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.SelectedIndex);
    }

    public class UIComboBoxGetterAdapter : IConfigurableControl
    {
        private readonly AntdUI.Select control;
        public UIComboBoxGetterAdapter(AntdUI.Select c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.SelectedIndex);
    }

    public class UITextBoxGetterAdapter : IConfigurableControl
    {
        private readonly AntdUI.Input control;
        public UITextBoxGetterAdapter(AntdUI.Input c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Text);
    }

    public class NumericUpDownGetterAdapter : IConfigurableControl
    {
        private readonly NumericUpDown control;
        public NumericUpDownGetterAdapter(NumericUpDown c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Value);
    }

    public class RadioButtonGetterAdapter : IConfigurableControl
    {
        private readonly RadioButton control;
        public RadioButtonGetterAdapter(RadioButton c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Checked);
    }
    public class UIRadioButtonGetterAdapter : IConfigurableControl
    {
        private readonly AntdUI.Radio control;
        public UIRadioButtonGetterAdapter(AntdUI.Radio c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Checked);
    }
    public class UITimePickerGetterAdapter : IConfigurableControl
    {
        private readonly AntdUI.TimePicker control;
        public UITimePickerGetterAdapter(AntdUI.TimePicker c) => control = c;
        public string Name => control.Name;
        public JsonNode? GetValue() => JsonValue.Create(control.Value.ToString());
    }
    // === Binder Adapters (LoadValue + BindEvent) ===
    public class TextBoxBinderAdapter : IControlAdapter
    {
        private readonly TextBox control;
        public TextBoxBinderAdapter(TextBox c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.Text = value?.ToString() ?? "";
        public void BindEvent(EventHandler handler) => control.TextChanged += handler;
    }

    public class UITextBoxBinderAdapter : IControlAdapter
    {
        private readonly AntdUI.Input control;
        public UITextBoxBinderAdapter(AntdUI.Input c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.Text = value?.ToString() ?? "";
        public void BindEvent(EventHandler handler) => control.TextChanged += handler;
    }

    public class CheckBoxBinderAdapter : IControlAdapter
    {
        private readonly CheckBox control;
        public CheckBoxBinderAdapter(CheckBox c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.Checked = value?.GetValue<bool>() ?? false;
        public void BindEvent(EventHandler handler) => control.CheckedChanged += handler;
    }

    public class UICheckBoxBinderAdapter : IControlAdapter
    {
        private readonly AntdUI.Checkbox control;
        public UICheckBoxBinderAdapter(AntdUI.Checkbox c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.Checked = value?.GetValue<bool>() ?? false;
        public void BindEvent(EventHandler handler)
        {
            control.CheckedChanged += (s, val) =>
            {
                handler?.Invoke(s, EventArgs.Empty);
            };
        }
    }


    public class ComboBoxBinderAdapter : IControlAdapter
    {
        private readonly ComboBox control;
        public ComboBoxBinderAdapter(ComboBox c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.SelectedIndex = value?.GetValue<int>() ?? 0;
        public void BindEvent(EventHandler handler) => control.SelectedIndexChanged += handler;
    }

    public class UIComboBoxBinderAdapter : IControlAdapter
    {
        private readonly AntdUI.Select control;
        public UIComboBoxBinderAdapter(AntdUI.Select c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.SelectedIndex = value?.GetValue<int>() ?? 0;
        public void BindEvent(EventHandler handler)
        {
            control.SelectedIndexChanged += (s, val) =>
            {
                handler?.Invoke(s, EventArgs.Empty);
            };
        }
    }

    public class RadioButtonBinderAdapter : IControlAdapter
    {
        private readonly RadioButton control;
        public RadioButtonBinderAdapter(RadioButton c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.Checked = value?.GetValue<bool>() ?? false;
        public void BindEvent(EventHandler handler) => control.CheckedChanged += handler;
    }
    public class UIRadioButtonBinderAdapter : IControlAdapter
    {
        private readonly AntdUI.Radio control;
        public UIRadioButtonBinderAdapter(AntdUI.Radio c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.Checked = value?.GetValue<bool>() ?? false;
        public void BindEvent(EventHandler handler)
        {
            control.CheckedChanged += (s, val) =>
            {
                handler?.Invoke(s, EventArgs.Empty);
            };
        }
    }
    public class NumericUpDownBinderAdapter : IControlAdapter
    {
        private readonly NumericUpDown control;
        public NumericUpDownBinderAdapter(NumericUpDown c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value) => control.Value = value?.GetValue<decimal>() ?? 0;
        public void BindEvent(EventHandler handler) => control.ValueChanged += handler;
    }
    public class UITimePickerBinderAdapter : IControlAdapter
    {
        private readonly AntdUI.TimePicker control;
        public UITimePickerBinderAdapter(AntdUI.TimePicker c) => control = c;
        public string Name => control.Name;
        public void LoadValue(JsonNode? value)
        {
            if (DateTime.TryParse(value?.ToString(), out var dateTimeValue))
                control.Value = dateTimeValue.TimeOfDay;
        }
        public void BindEvent(EventHandler handler)
        {
            control.ValueChanged += (s, val) =>
            {
                handler?.Invoke(s, EventArgs.Empty);
            };
        }
    }
}
