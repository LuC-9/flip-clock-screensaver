using System;
using System.Globalization;
using Microsoft.Win32;

namespace FliqloClock
{
    public class Settings
    {
        private const string RegistryKeyPath = @"Software\FliqloClockCS";

        public bool Is24Hour { get; set; }
        public bool ShowAmPm { get; set; }
        public bool ShowSeconds { get; set; }
        public float ClockScale { get; set; }

        public Settings()
        {
            Is24Hour = true;
            ShowAmPm = false;
            ShowSeconds = true;
            ClockScale = 1.0f;
        }

        public void Load()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        object is24HourVal = key.GetValue("Is24Hour");
                        if (is24HourVal != null)
                        {
                            Is24Hour = Convert.ToBoolean(is24HourVal);
                        }

                        object showAmPmVal = key.GetValue("ShowAmPm");
                        if (showAmPmVal != null)
                        {
                            ShowAmPm = Convert.ToBoolean(showAmPmVal);
                        }

                        object showSecondsVal = key.GetValue("ShowSeconds");
                        if (showSecondsVal != null)
                        {
                            ShowSeconds = Convert.ToBoolean(showSecondsVal);
                        }

                        object clockScaleVal = key.GetValue("ClockScale");
                        if (clockScaleVal != null)
                        {
                            float scale;
                            if (float.TryParse(clockScaleVal.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out scale))
                            {
                                if (scale >= 0.5f && scale <= 1.5f)
                                {
                                    ClockScale = scale;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to defaults on error
            }
        }

        public void Save()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("Is24Hour", Is24Hour ? 1 : 0, RegistryValueKind.DWord);
                        key.SetValue("ShowAmPm", ShowAmPm ? 1 : 0, RegistryValueKind.DWord);
                        key.SetValue("ShowSeconds", ShowSeconds ? 1 : 0, RegistryValueKind.DWord);
                        key.SetValue("ClockScale", ClockScale.ToString("0.0", CultureInfo.InvariantCulture), RegistryValueKind.String);
                    }
                }
            }
            catch (Exception)
            {
                // Fail silently on save error
            }
        }
    }
}
