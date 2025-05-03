using System.ComponentModel;
using System.Globalization;
using System.Resources;
using GM4ManagerWPF.Properties;

namespace GM4ManagerWPF.Localization
{
    public class ResourceService : INotifyPropertyChanged
    {
        public static ResourceService Instance { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly ResourceManager _resourceManager = Resources.ResourceManager;

        public string this[string key] => _resourceManager.GetString(key) ?? $"!{key}!";

        public void ChangeCulture(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            Resources.Culture = culture;
            RaiseLanguageChanged();
        }

        public void RaiseLanguageChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
        public ResourceService() { }
    }
}
