using System.Windows;
using GM4ManagerWPF.Classes;

namespace GM4ManagerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SplashScreenWindow splashScreen;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show splash
            splashScreen = new SplashScreenWindow();
            splashScreen.Show();

            try
            {
                // Simulate config loading
                AppSettingsManager.Load(); // or await if async
                await Task.Delay(800); // Simulate loading time
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load settings:\n" + ex.Message);
                Shutdown();
                return;
            }

            // Show main window
            var mainWindow = new MainWindow();
            mainWindow.Loaded += (s, ev) =>
            {
                // Fade out splash screen after main window is shown
                splashScreen.BeginFadeOut(() =>
                {
                    splashScreen.Close();
                });
            };

            mainWindow.Show(); // Triggers Loaded event
        }
    }   
}
