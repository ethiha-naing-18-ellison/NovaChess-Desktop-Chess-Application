using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaChess.App.Views;

namespace NovaChess.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Nova Chess - Desktop";
    
    [ObservableProperty]
    private string _subtitle = "Your premium chess experience";
    
    public IAsyncRelayCommand NewGameCommand { get; }
    public IAsyncRelayCommand AnalysisCommand { get; }
    
    public HomeViewModel()
    {
        NewGameCommand = new AsyncRelayCommand(StartNewGameAsync);
        AnalysisCommand = new AsyncRelayCommand(StartAnalysisAsync);
    }
    
    private async Task StartNewGameAsync()
    {
        try
        {
            var dialog = new NewGameDialog();
            if (dialog.ShowDialog() == true)
            {
                // Navigate to game view with the selected configuration
                // This will be handled by the MainWindow navigation
                MessageBox.Show("New game configuration selected! Navigate to Game view to start playing.", 
                              "New Game", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error starting new game: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task StartAnalysisAsync()
    {
        try
        {
            // Navigate to analysis view
            MessageBox.Show("Analysis mode selected! Navigate to Analysis view to start analyzing positions.", 
                          "Analysis Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error starting analysis: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
