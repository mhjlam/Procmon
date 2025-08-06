using System;
using Procmon.Models;

namespace Procmon.Helpers
{
    /// <summary>
    /// Helper class for UI initialization and safety checks
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Safely update settings with null checks
        /// </summary>
        public static void SafeUpdateSettings(MonitoringSettings settings, Action<MonitoringSettings> updateAction)
        {
            if (settings != null && updateAction != null)
            {
                try
                {
                    updateAction(settings);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating settings: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Safely get text from a TextBox with fallback value
        /// </summary>
        public static string SafeGetText(System.Windows.Controls.TextBox textBox, string defaultValue = "")
        {
            try
            {
                return textBox?.Text ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely set text on a TextBox
        /// </summary>
        public static void SafeSetText(System.Windows.Controls.TextBox textBox, string text)
        {
            try
            {
                if (textBox != null)
                {
                    textBox.Text = text ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting text: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely get checkbox state
        /// </summary>
        public static bool SafeGetIsChecked(System.Windows.Controls.CheckBox checkBox, bool defaultValue = false)
        {
            try
            {
                return checkBox?.IsChecked == true;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely parse integer from TextBox
        /// </summary>
        public static bool SafeTryParseInt(System.Windows.Controls.TextBox textBox, out int result)
        {
            result = 0;
            try
            {
                return int.TryParse(textBox?.Text, out result);
            }
            catch
            {
                return false;
            }
        }
    }
}