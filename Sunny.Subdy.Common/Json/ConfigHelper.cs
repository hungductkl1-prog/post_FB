using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Json
{
    public class ConfigHelper
    {
        private readonly JObject jConfig = new();
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
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs");
            FileHelper.CreateFolder(folder);
            return Path.Combine(folder, $"{filename}.json");
        }
        private void LoadConfigFromFile(string content)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var parsed = JObject.Parse(content);
                    foreach (var prop in parsed)
                        jConfig[prop.Key] = prop.Value;
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
                        var parsed = JObject.Parse(content);
                        foreach (var prop in parsed)
                            jConfig[prop.Key] = prop.Value;
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

                if (jConfig.TryGetValue(adapter.Name, out var value))
                {
                    try { adapter.LoadValue(value); } catch (Exception ex) { LogManager.Error(ex); }
                }

                adapter.BindEvent(ValueChanged);
            }

            onLoadAction?.Invoke();
        }

        public void ControlClosing(object? sender, EventArgs e)
        {
            SaveAllControlValues();

            if (!string.IsNullOrEmpty(configFile))
            {
                try { File.WriteAllText(configFile!, jConfig.ToString()); } catch (Exception ex) { LogManager.Error(ex); }
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
                        jConfig[adapter.Name] = JToken.FromObject(value);
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
                    jConfig[adapter.Name] = JToken.FromObject(value);
                    if (!string.IsNullOrEmpty(configFile))
                        File.WriteAllText(configFile!, jConfig.ToString());
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

        public void AddValue(string key, object value)
        {
            try { jConfig[key] = JToken.FromObject(value); }
            catch (Exception ex) { LogManager.Error(ex); }
        }

        public string GetJsonString()
        {
            SaveAllControlValues();
            return jConfig.ToString(Formatting.None);
        }
    }

}
