using System.Text.Json;
using System.Text.Json.Nodes;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Json
{
    public class ConfigHelper
    {
        private static readonly JsonSerializerOptions _indentedOptions = new() { WriteIndented = true };

        private readonly JsonObject jConfig = new();
        private readonly Form? form;
        private readonly UserControl? uc;
        private readonly string? configFile;
        private readonly List<Control> excepts;
        private readonly Action? onLoadAction;
        private readonly Action? onCloseAction;
        private readonly bool shouldExit;

        public ConfigHelper(Form form, string configFilename, List<Control>? excepts = null, Action? onLoad = null, Action? onClose = null, bool shouldExit = true)
        {
            this.form = form;
            this.excepts = excepts ?? new();
            this.onLoadAction = onLoad;
            this.onCloseAction = onClose;
            this.shouldExit = shouldExit;
            configFilename = string.Concat(configFilename.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            configFile = InitConfigFile(configFilename);
            LoadConfigFromFile();

            form.Load += ControlLoad;
            form.FormClosing += ControlClosing;
        }
        public ConfigHelper(Form form, string jsonString)
        {
            this.form = form;
            this.excepts = excepts ?? new();
            this.shouldExit = false;
            LoadConfigFromFile(jsonString);

            form.Load += ControlLoad;
        }
        public ConfigHelper(UserControl uc, string configFilename, List<Control>? excepts = null, Action? onLoad = null, Action? onClose = null)
        {
            this.uc = uc;
            this.excepts = excepts ?? new();
            this.onLoadAction = onLoad;
            this.onCloseAction = onClose;

            configFile = InitConfigFile(configFilename);
            LoadConfigFromFile();

            uc.Load += ControlLoad;
            uc.Disposed += ControlClosing;
        }

        private string InitConfigFile(string filename)
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "configs");
            FileHelper.CreateFolder(folder);
            return Path.Combine(folder, $"{filename}.json");
        }
        private void LoadConfigFromFile(string content)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var parsed = JsonNode.Parse(content)?.AsObject();
                    if (parsed != null)
                    {
                        foreach (var prop in parsed)
                            jConfig[prop.Key] = prop.Value?.DeepClone();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
        }
        private void LoadConfigFromFile()
        {
            try
            {
                if (File.Exists(configFile))
                {
                    var content = File.ReadAllText(configFile!);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var parsed = JsonNode.Parse(content)?.AsObject();
                        if (parsed != null)
                        {
                            foreach (var prop in parsed)
                                jConfig[prop.Key] = prop.Value?.DeepClone();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
        }

        private void ControlLoad(object? sender, EventArgs e)
        {
            var controls = GetAllControls();
            foreach (var control in controls)
            {
                if (string.IsNullOrEmpty(control.Name) || excepts.Contains(control))
                    continue;

                var adapter = CreateBinderAdapter(control);
                if (adapter == null)
                    continue;

                if (jConfig.TryGetPropertyValue(adapter.Name, out var value))
                {
                    try { adapter.LoadValue(value); } catch (Exception ex) { LogManager.Error(ex); }
                }

                adapter.BindEvent(ValueChanged);
            }

            onLoadAction?.Invoke();
        }

        private bool _saved = false;
        public void ControlClosing(object? sender, EventArgs e)
        {
            if (_saved) return;
            _saved = true;

            SaveAllControlValues();

            if (!string.IsNullOrEmpty(configFile))
            {
                try { File.WriteAllText(configFile!, jConfig.ToJsonString(_indentedOptions)); } catch (Exception ex) { LogManager.Error(ex); }
            }

            onCloseAction?.Invoke();

            if (shouldExit && form != null)
                Environment.Exit(0);
        }

        private void SaveAllControlValues()
        {
            var controls = GetAllControls();
            foreach (var control in controls)
            {
                if (string.IsNullOrEmpty(control.Name) || excepts.Contains(control))
                    continue;

                var adapter = CreateGetterAdapter(control);
                if (adapter == null)
                    continue;

                try
                {
                    var value = adapter.GetValue();
                    if (value != null)
                        jConfig[adapter.Name] = value;
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex);
                }
            }

        }

        private void ValueChanged(object? sender, EventArgs e)
        {
            if (sender is not Control control || string.IsNullOrEmpty(control.Name) || excepts.Contains(control))
                return;

            var adapter = CreateGetterAdapter(control);
            if (adapter == null)
                return;

            try
            {
                var value = adapter.GetValue();
                if (value != null)
                {
                    jConfig[adapter.Name] = value;
                    if (!string.IsNullOrEmpty(configFile))
                        File.WriteAllText(configFile!, jConfig.ToJsonString(_indentedOptions));
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
        }

        private List<Control> GetAllControls()
        {
            return form != null ? ControlHelper.GetControls(form) : uc != null ? ControlHelper.GetControls(uc) : new();
        }

        private IControlAdapter? CreateBinderAdapter(Control control)
        {
            if (control is AntdUI.Checkbox antChk) return new UICheckBoxBinderAdapter(antChk);
            if (control is System.Windows.Forms.CheckBox chk) return new CheckBoxBinderAdapter(chk);

            if (control is AntdUI.Input antInput) return new UITextBoxBinderAdapter(antInput);
            if (control is System.Windows.Forms.TextBox tb) return new TextBoxBinderAdapter(tb);

            if (control is AntdUI.Select antSelect) return new UIComboBoxBinderAdapter(antSelect);
            if (control is System.Windows.Forms.ComboBox combo) return new ComboBoxBinderAdapter(combo);

            if (control is AntdUI.Radio antRadio) return new UIRadioButtonBinderAdapter(antRadio);
            if (control is System.Windows.Forms.RadioButton radio) return new RadioButtonBinderAdapter(radio);

            if (control is AntdUI.TimePicker timePicker) return new UITimePickerBinderAdapter(timePicker);
            if (control is System.Windows.Forms.NumericUpDown num) return new NumericUpDownBinderAdapter(num);

            return null;
        }
        private IConfigurableControl? CreateGetterAdapter(Control control)
        {
            if (control is AntdUI.Input uitb)
                return new UITextBoxGetterAdapter(uitb);
            if (control is TextBox tb)
                return new TextBoxGetterAdapter(tb);

            if (control is AntdUI.Checkbox antChk)
                return new UICheckBoxGetterAdapter(antChk);
            if (control is CheckBox chk)
                return new CheckBoxGetterAdapter(chk);

            if (control is AntdUI.Select select)
                return new UIComboBoxGetterAdapter(select);
            if (control is ComboBox combo)
                return new ComboBoxGetterAdapter(combo);

            if (control is AntdUI.Radio uirb)
                return new UIRadioButtonGetterAdapter(uirb);
            if (control is RadioButton rb)
                return new RadioButtonGetterAdapter(rb);

            if (control is AntdUI.TimePicker picker)
                return new UITimePickerGetterAdapter(picker);
            if (control is NumericUpDown nud)
                return new NumericUpDownGetterAdapter(nud);

            return null;
        }

        public void AddValue(string key, bool value) => jConfig[key] = JsonValue.Create(value);
        public void AddValue(string key, int value) => jConfig[key] = JsonValue.Create(value);
        public void AddValue(string key, decimal value) => jConfig[key] = JsonValue.Create(value);
        public void AddValue(string key, string? value) => jConfig[key] = JsonValue.Create(value);
        public void AddValue(string key, object? value)
        {
            jConfig[key] = value switch
            {
                bool b => JsonValue.Create(b),
                int i => JsonValue.Create(i),
                long l => JsonValue.Create(l),
                decimal d => JsonValue.Create(d),
                double db => JsonValue.Create(db),
                float f => JsonValue.Create(f),
                string s => JsonValue.Create(s),
                _ => JsonValue.Create(value?.ToString())
            };
        }

        public string GetJsonString()
        {
            SaveAllControlValues();
            return jConfig.ToJsonString();
        }
    }

}
