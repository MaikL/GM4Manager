using GM4ManagerWPF.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GM4ManagerWPF.ViewModels
{
    public class ExplorerUCViewModel: INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;
        public ExplorerUCViewModel()
        {

        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
