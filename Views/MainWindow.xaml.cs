using System.Windows;
using GM4ManagerWPF.Classes;

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