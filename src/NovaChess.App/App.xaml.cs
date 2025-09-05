using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaChess.Infrastructure.Interfaces;
using NovaChess.Infrastructure.Services;
using NovaChess.App.ViewModels;
using NovaChess.App.Views;
using System.Windows;
using System; // Added for InvalidOperationException

namespace NovaChess.App;

public partial class App : Application
{
    private static IHost? _host; // Changed to static
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = CreateHostBuilder(e.Args).Build();
        await _host.StartAsync();
        
        var mainWindow = new MainWindow
        {
            DataContext = _host.Services.GetRequiredService<MainWindowViewModel>()
        };
        
        mainWindow.Show();
        
        base.OnStartup(e);
    }
    
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.OnExit(e);
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<IEngineService, EngineService>();
                
                // Register ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddSingleton<GameViewModel>();
                services.AddTransient<AnalysisViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<PgnLibraryViewModel>();
                
                // Register Views
                services.AddTransient<HomeView>();
                services.AddTransient<GameView>();
                services.AddTransient<AnalysisView>();
                services.AddTransient<SettingsView>();
                services.AddTransient<PgnLibraryView>();
            });
    
    public static IServiceProvider Services => _host?.Services ?? 
        throw new InvalidOperationException("Host not initialized");
}
