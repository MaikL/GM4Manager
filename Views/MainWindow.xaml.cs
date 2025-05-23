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
            var sw = Stopwatch.StartNew();

            InitializeComponent();

            this.DataContext = new MainWindowViewModel();

            if (AppSettingsManager.Settings.Theme == "Dark")
            {
                SetThemeDark(null, null);
            }
            else
            {
                SetThemeLight(null, null);
            }

            sw.Stop();
            Debug.WriteLine($"MainWindow dauerte: {sw.ElapsedMilliseconds} ms");
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