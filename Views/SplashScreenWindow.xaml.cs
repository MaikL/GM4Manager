using GM4ManagerWPF.ViewModels;
using System.Windows;

namespace GM4ManagerWPF
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenViewModel ViewModel { get; } = new SplashScreenViewModel();
        public SplashScreenWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
        public void UpdateStatus(string message)
        {
            ViewModel.GroupLoadingStatus = message;
        }
        public void BeginFadeOut(Action onComplete)
        {
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => onComplete?.Invoke();
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }
    }
}
