using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace GM4ManagerWPF.Classes
{
    public static class AppSettingsManager
    {
        // Backing field for the loaded settings instance
        private static AppSettings? _settings;

        // Configuration file name
        private static readonly string SettingsFileName = "appsettings.json";

        // Flag to prevent reloading
        private static bool _isLoaded = false;

        // Public accessor for app settings
        public static AppSettings Settings
        {
            get
            {
                if (IsInDesignMode)
                {
                    return _settings ??= GetDesignTimeDefaults();
                }

                if (!_isLoaded)
                {
                    Load();
                }

                return _settings!;
            }
        }

        // Detect if the app is running in design mode (e.g., Visual Studio Designer)
        public static bool IsInDesignMode =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime ||
            AppDomain.CurrentDomain.FriendlyName.Contains("devenv", StringComparison.OrdinalIgnoreCase) ||
            Environment.CommandLine.Contains("VisualStudio", StringComparison.OrdinalIgnoreCase);

        // Loads the settings from the JSON file
        public static void Load()
        {
            string path = GetSettingsFilePath();

            if (!File.Exists(path))
            {
                _settings = GetDesignTimeDefaults();
                Save();
                //throw new FileNotFoundException("Configuration file not found.", path);
            }

            string json = File.ReadAllText(path);
            _settings = JsonSerializer.Deserialize<AppSettings>(json)
                        ?? throw new InvalidOperationException("Failed to deserialize appsettings.json.");

            _isLoaded = true;
        }

        // Builds the path to the settings file depending on runtime or design-time
        private static string GetSettingsFilePath()
        {
            string baseDir = IsInDesignMode
                ? Environment.CurrentDirectory
                : AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(baseDir, SettingsFileName);
        }

        // Default settings used during design-time (to avoid null references)
        private static AppSettings GetDesignTimeDefaults() => new()
        {
            Domain = "example.local",
            OverwriteCurrentDomain = false,
            StartShare = @"\\server\share",
            UserLanguage = "en",
            Theme = "Light"
        };

        // Changes the current language and saves it to settings
        public static void SetLanguage(string languageCode)
        {
            Debug.WriteLine($"saving language: {languageCode}");
            // Update setting
            Settings.UserLanguage = languageCode;
            // Save updated settings
            Save();
        }
        // Changes the current theme and saves it to settings
        public static void SetTheme(string theme)
        {
            Debug.WriteLine($"saving theme: {theme}");
            // Update setting
            Settings.Theme = theme;
            // Save updated settings
            Save();
        }

        // Saves the current settings back to the JSON file
        public static void Save()
        {
            string path = GetSettingsFilePath();
            Debug.WriteLine($"saving file: {path}");
            string json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(path, json);
        }

        // Call this on startup to set language from saved config
        public static void ApplySavedLanguage()
        {
            if (!_isLoaded)
            {
                Load();
            }

            string language = Settings.UserLanguage;
            var culture = new System.Globalization.CultureInfo(language);
            if (CultureInfo.CurrentUICulture == CultureInfo.InvariantCulture)
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            }
            else
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
        }
        public static void ApplyUserCulture()
        {
            if (!_isLoaded)
            {
                Load();
            }

            string languageCode = Settings.UserLanguage;

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                try
                {
                    var culture = new CultureInfo(languageCode);
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
                catch (CultureNotFoundException)
                {
                    // Fallback: Englische Standardsprache
                    var fallbackCulture = new CultureInfo("en");
                    Thread.CurrentThread.CurrentCulture = fallbackCulture;
                    Thread.CurrentThread.CurrentUICulture = fallbackCulture;
                }
            }
        }
    }
}