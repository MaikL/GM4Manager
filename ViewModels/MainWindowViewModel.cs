using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Properties;
using GM4ManagerWPF.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
namespace GM4ManagerWPF.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public static ResourceService Res => ResourceService.Instance;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ExplorerUCViewModel Explorer { get; } = new ExplorerUCViewModel();
        //public ManagerUCViewModel Manager { get; } = new ManagerUCViewModel();
        public MainWindowViewModel()
        {
            var sw = Stopwatch.StartNew();

            Explorer = new ExplorerUCViewModel();

            //Manager = new ManagerUCViewModel();   

            sw.Stop();
            Debug.WriteLine($"MainWindowViewModel Konstruktor dauerte: {sw.ElapsedMilliseconds} ms");
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public async Task InitializeAsync(Action<string>? reportStatus = null)
        {
            await Explorer.InitializeAsync(reportStatus);
        }

    }
}