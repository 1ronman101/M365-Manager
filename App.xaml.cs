using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using M365Manager.Helpers;
using M365Manager.Services;
using M365Manager.ViewModels;
using M365Manager.Views;
using Serilog;
using System.Runtime.InteropServices;

namespace M365Manager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window MainWindow { get; private set; } = null!;
    public static AppSettings Settings { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception
        Log.Error(e.Exception, "Unhandled exception");

        // Show error to user
        NativeMethods.MessageBox(IntPtr.Zero, 
            $"An unexpected error occurred:\n\n{e.Exception.Message}", 
            "M365 Manager Error", 
            0x10);

        e.Handled = true;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Settings = AppSettings.Load();

            // Use AppData for logs (writable without admin)
            var logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "M365Manager", "logs");
            Directory.CreateDirectory(logFolder);
            var logPath = Path.Combine(logFolder, "m365manager-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .CreateLogger();

            Log.Information("Application starting...");

            // Build DI container.
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();

            // Initialize auth service.
            var authService = Services.GetRequiredService<AuthService>();
            if (Settings.IsConfigured)
            {
                authService.Initialize(Settings);
            }

            MainWindow = new MainWindow();
            MainWindow.Activate();

            Log.Information("Application started successfully.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            NativeMethods.MessageBox(IntPtr.Zero,
                $"Failed to start application:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "M365 Manager - Startup Error",
                0x10);
            throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Services (singletons so auth state is shared)
        services.AddSingleton<AuthService>();
        services.AddSingleton<GraphUserService>();
        services.AddSingleton<GraphGroupService>();
        services.AddSingleton<LicenseService>();

        // ViewModels (transient so each navigation gets a fresh instance)
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CreateUserViewModel>();
        services.AddTransient<ManageUserViewModel>();
        services.AddTransient<GroupsViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}
