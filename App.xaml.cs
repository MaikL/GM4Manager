using GM4ManagerWPF.Classes;
using GM4ManagerWPF.Helpers;
using GM4ManagerWPF.Interfaces;
using GM4ManagerWPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;


namespace GM4ManagerWPF
{
    /// <summary>  
    /// Interaction logic for App.xaml  
    /// </summary>  
    public partial class App : Application
    {

        public static ServiceProvider Services { get; private set; } = null!;

        private SplashScreenWindow? splashScreen;


        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var sw = Stopwatch.StartNew();

            // Splash anzeigen
            splashScreen = new SplashScreenWindow();
            splashScreen.Show();

            var groups = await ActiveDirectoryService.GetUserGroupsForCurrentUserAsync(status =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    splashScreen.UpdateStatus(status);
                });
            });


            try
            {
                AppSettingsManager.Load();
                await Task.Delay(800);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load settings:\n" + ex.Message);
                Shutdown();
                return;
            }

            
            var services = new ServiceCollection();
            services.AddSingleton<ICursorService, CursorService>();
            services.AddSingleton<MainWindowViewModel>();
            Services = services.BuildServiceProvider();
            
            var mainWindow = new MainWindow(Services.GetRequiredService<MainWindowViewModel>());
            Application.Current.MainWindow = mainWindow;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            mainWindow.Loaded += (s, ev) =>
            {
                splashScreen?.BeginFadeOut(() =>
                {
                    splashScreen?.Close();
                    splashScreen = null;
                });
            };

            sw.Stop();
            Debug.WriteLine($"Splash dauerte: {sw.ElapsedMilliseconds} ms");

            mainWindow.Show();
        }
    }
}
