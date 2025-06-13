using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Views;
using ModernWpf;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GM4ManagerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel? viewModel;
        public MainWindow(MainWindowViewModel viewModel)
        {
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Debug.WriteLine("MainWindow constructor called");
            var sw = Stopwatch.StartNew();

            InitializeComponent();
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
            Debug.WriteLine($"ExplorerUC geladen: {viewModel.ExplorerUC != null}");
            Debug.WriteLine($"ExplorerContentHost.Children.Count: {ExplorerContentHost.Children.Count}");

            DataContext = viewModel;            

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
        private async void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"TabIndex: {MainTabControl.SelectedIndex}");
            Debug.WriteLine($"ExplorerUC loaded: {viewModel?.ExplorerUC != null}");
            Debug.WriteLine($"ExplorerContentHost.Children.Count: {ExplorerContentHost.Children.Count}");

            if (viewModel == null)
            {
                Debug.WriteLine("viewModel is null, skipping LoadExplorerAsync.");
                return;
            }

            if (viewModel.ExplorerUC == null)
            {
                await viewModel.LoadExplorerAsync();
            }

            if (viewModel.ExplorerUC != null && ExplorerContentHost.Children.Count == 0)
            {
                var explorerView = new ExplorerUC(viewModel.ExplorerUC);
                ExplorerContentHost.Children.Clear();
                ExplorerContentHost.Children.Add(explorerView);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
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