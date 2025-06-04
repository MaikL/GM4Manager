using GM4ManagerWPF.Classes;
using GM4ManagerWPF.ViewModels;
using ModernWpf;
using System.Diagnostics;
using System.Windows;

namespace GM4ManagerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Debug.WriteLine("MainWindow constructor called");
            var sw = Stopwatch.StartNew();

            InitializeComponent();

            this.DataContext = new MainWindowViewModel();

            Loaded += async (_, __) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    await vm.InitializeAsync(msg => Console.WriteLine(msg)); // optional status reporting
                }
            };

            if (AppSettingsManager.Settings.Theme == "Dark")
            {
                SetThemeDark(null, null);
            }
            else
            {
                SetThemeLight(null, null);
            }

            sw.Stop();
            Debug.WriteLine($"MainWindow took: {sw.ElapsedMilliseconds} ms");
        }
        private void SetThemeDark(object? sender, RoutedEventArgs? e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            AppSettingsManager.SetTheme("Dark");
        }
        private void SetThemeLight(object? sender, RoutedEventArgs? e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            AppSettingsManager.SetTheme("Light");
        }

        private void SetLanguage(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string languageCode)
            {
                LanguageService.ChangeLanguage(languageCode);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("DataContext: " + this.DataContext?.GetType().Name);
        }

    }
}