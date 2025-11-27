using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media;

namespace AutoGameHDR
{
    public enum AppTheme
    {
        Auto,
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public static AppTheme CurrentThemePreference { get; set; } = AppTheme.Auto;

        public static void ApplyTheme(AppTheme theme)
        {
            CurrentThemePreference = theme;
            bool isDark;

            if (theme == AppTheme.Dark) isDark = true;
            else if (theme == AppTheme.Light) isDark = false;
            else isDark = IsSystemInDarkMode();

            if (isDark)
            {
                // 深色模式
                SetResource("WindowBackground", "#1E1E1E");
                SetResource("ControlBackground", "#2D2D30");
                SetResource("PrimaryText", "#FFFFFF");
                SetResource("SecondaryText", "#AAAAAA");
                SetResource("BorderBrush", "#3E3E42");

                // 菜单专用 (深灰)
                SetResource("MenuBackground", "#1B1B1C");
                SetResource("MenuBorder", "#333337");
                SetResource("MenuItemHover", "#333337");
            }
            else
            {
                // 浅色模式
                SetResource("WindowBackground", "#FFFFFF");
                SetResource("ControlBackground", "#F5F5F5");
                SetResource("PrimaryText", "#000000");
                SetResource("SecondaryText", "#666666");
                SetResource("BorderBrush", "#CCCCCC");

                // 菜单专用 (白)
                SetResource("MenuBackground", "#FFFFFF");
                SetResource("MenuBorder", "#CCCCCC");
                SetResource("MenuItemHover", "#F0F0F0");
            }

            // 【关键】强制按钮始终为 Windows 蓝色，不随主题变灰
            SetResource("ButtonBackground", "#0078D7");
            SetResource("ButtonHover", "#198CE6");
            SetResource("ButtonText", "#FFFFFF");
        }

        private static void SetResource(string key, string hexColor)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var newBrush = new SolidColorBrush(color);
                Application.Current.Resources[key] = newBrush;
            }
            catch { }
        }

        private static bool IsSystemInDarkMode()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    object val = key?.GetValue(RegistryValueName);
                    return val != null && (int)val == 0;
                }
            }
            catch { return false; }
        }
    }
}