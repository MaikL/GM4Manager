using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using GM4ManagerWPF.Localization;

namespace GM4ManagerWPF.Classes
{
    public static class LanguageService
    { 
        // Event that will be raised when language is changed
        public static event EventHandler? LanguageChanged;
        /// <summary>
        /// Changes the application language to the specified culture code.
        /// </summary>
        /// <param name="cultureCode">The culture code to change the language to.</param>
        public static void ChangeLanguage(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            AppSettingsManager.SetLanguage(cultureCode);
            AppSettingsManager.Save();
            ResourceService.Instance.RaiseLanguageChanged();

            // Notify all listeners (e.g., views) about the change
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
        /// <summary>
        /// Gets the list of available languages by checking for satellite assemblies.
        /// </summary>
        /// <returns>A list of available languages as CultureInfo objects.</returns>
        public static List<CultureInfo> GetAvailableLanguages()
        {
            var languages = new List<CultureInfo>();
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            foreach (string dir in Directory.GetDirectories(exePath))
            {
                try
                {
                    string cultureName = new DirectoryInfo(dir).Name;

                    var culture = CultureInfo.GetCultureInfo(cultureName);

                    Debug.WriteLine($"\n{Assembly.GetExecutingAssembly().GetName().Name}.resources.dll");
                    // Check if satellite assembly exists
                    string resourceDll = Path.Combine(dir, $"{Assembly.GetExecutingAssembly().GetName().Name}.resources.dll");
                    if (File.Exists(resourceDll))
                    {
                        languages.Add(culture);
                    }
                }
                catch (CultureNotFoundException)
                {
                    // Ignore folders that are not valid cultures
                }
            }

            // Optional: Ensure "default" language is in the list (neutral culture)
            languages.Insert(0, CultureInfo.InvariantCulture); // e.g. fallback to default Resources.resx
            return languages;
        }
    }
}