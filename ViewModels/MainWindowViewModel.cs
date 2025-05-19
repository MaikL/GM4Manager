using GM4ManagerWPF.Localization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GM4ManagerWPF.Properties;
namespace GM4ManagerWPF.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
