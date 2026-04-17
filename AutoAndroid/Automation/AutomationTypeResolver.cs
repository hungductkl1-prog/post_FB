using System;

namespace AutoAndroid
{
    public enum AutomationType
    {
        Ui2Automation = 0,
        Appium = 1
    }

    public static class AutomationTypeResolver
    {
        public const string DefaultType = "ui2automation";

        public static AutomationType Parse(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return AutomationType.Ui2Automation;
            }

            string normalized = type.Trim().ToLowerInvariant();
            return normalized switch
            {
                "ui2automation" => AutomationType.Ui2Automation,
                "ui2" => AutomationType.Ui2Automation,
                "uiautomator2" => AutomationType.Ui2Automation,
                "atx" => AutomationType.Ui2Automation,
                "appium" => AutomationType.Appium,
                _ => AutomationType.Ui2Automation
            };
        }

        public static string Normalize(AutomationType type)
        {
            return type == AutomationType.Appium ? "appium" : DefaultType;
        }
    }
}
