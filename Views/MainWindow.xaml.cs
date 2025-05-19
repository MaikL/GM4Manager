using GM4ManagerWPF.Classes;
using GM4ManagerWPF.ViewModels;
using ModernWpf;
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
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }
        private void SetThemeDark(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            AppSettingsManager.SetTheme("Dark");
        }
        private void SetThemeLight(object sender, RoutedEventArgs e)
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
    }
}