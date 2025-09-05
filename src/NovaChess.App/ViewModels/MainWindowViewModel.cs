using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NovaChess.App.Views;
using NovaChess.App.ViewModels;

namespace NovaChess.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentView;
    
    [ObservableProperty]
    private string _currentViewTitle = "Home";
    
    public ObservableCollection<RecentGame> RecentGames { get; } = new();
    
    public IRelayCommand NavigateHomeCommand { get; }
    public IRelayCommand NavigateGameCommand { get; }
    public IRelayCommand NavigateAnalysisCommand { get; }
    public IRelayCommand NavigateSettingsCommand { get; }
    public IRelayCommand ShowSettingsCommand { get; }
    public IRelayCommand ExitCommand { get; }
    public IRelayCommand LoadGameCommand { get; }
    
    public MainWindowViewModel()
    {
        // Initialize commands
        NavigateHomeCommand = new RelayCommand(() => NavigateToView("Home"));
        NavigateGameCommand = new RelayCommand(() => NavigateToView("Game"));
        NavigateAnalysisCommand = new RelayCommand(() => NavigateToView("Analysis"));
        NavigateSettingsCommand = new RelayCommand(() => NavigateToView("Settings"));
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        ExitCommand = new RelayCommand(Exit);
        LoadGameCommand = new RelayCommand<RecentGame>(LoadGame);
        
        // Set initial view
        NavigateToView("Home");
        
        // Load recent games
        LoadRecentGames();
    }
    
    private void NavigateToView(string viewName)
    {
        CurrentViewTitle = viewName;
        
        CurrentView = viewName switch
        {
            "Home" => new HomeView { DataContext = App.Services.GetRequiredService<HomeViewModel>() },
            "Game" => new GameView { DataContext = App.Services.GetRequiredService<GameViewModel>() },
            "Analysis" => new AnalysisView { DataContext = App.Services.GetRequiredService<AnalysisViewModel>() },
            "Settings" => new SettingsView { DataContext = App.Services.GetRequiredService<SettingsViewModel>() },
            _ => new HomeView { DataContext = App.Services.GetRequiredService<HomeViewModel>() }
        };
    }
    
    private void ShowSettings()
    {
        var settingsView = new SettingsView { DataContext = App.Services.GetRequiredService<SettingsViewModel>() };
        var settingsWindow = new Window
        {
            Title = "Settings",
            Content = settingsView,
            Width = 600,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow
        };
        
        settingsWindow.ShowDialog();
    }
    
    private void Exit()
    {
        Application.Current.Shutdown();
    }
    
    private void LoadGame(RecentGame? game)
    {
        if (game == null) return;
        
        // Navigate to game view and load the saved game
        NavigateToView("Game");
        
        // TODO: Implement loading saved games
        MessageBox.Show($"Loading game: {game.Title}", "Load Game", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void LoadRecentGames()
    {
        // TODO: Load recent games from storage
        // For now, add some sample games
        RecentGames.Add(new RecentGame { Title = "Game 1", Date = "2024-01-01", Result = "White wins" });
        RecentGames.Add(new RecentGame { Title = "Game 2", Date = "2024-01-02", Result = "Draw" });
    }
}

public class RecentGame
{
    public string Title { get; set; } = "";
    public string Date { get; set; } = "";
    public string Result { get; set; } = "";
}
