using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GM4ManagerWPF.ViewModels
{
    public class SplashScreenViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        private string _groupLoadingStatus = Resources.startingText;
        public string GroupLoadingStatus
        {
            get => _groupLoadingStatus;
            set
            {
                _groupLoadingStatus = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
