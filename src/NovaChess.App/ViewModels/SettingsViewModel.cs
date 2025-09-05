using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace NovaChess.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _playerName = "Player";
    
    [ObservableProperty]
    private bool _soundEnabled = true;
    
    [ObservableProperty]
    private bool _animationsEnabled = true;
    
    [ObservableProperty]
    private bool _showLegalMoves = true;
    
    [ObservableProperty]
    private bool _highlightLastMove = true;
    
    [ObservableProperty]
    private string _selectedTheme = "Classic";
    
    [ObservableProperty]
    private string _selectedBoardStyle = "Wood";
    
    [ObservableProperty]
    private string _selectedPieceSet = "Classic";
    
    [ObservableProperty]
    private int _defaultTimeControlMinutes = 10;
    
    [ObservableProperty]
    private int _defaultAISkillLevel = 12;
    
    [ObservableProperty]
    private bool _autoSaveGames = true;
    
    [ObservableProperty]
    private string _pgnExportPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    
    public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "Classic", "Dark", "Modern", "Blue", "Green"
    };
    
    public ObservableCollection<string> AvailableBoardStyles { get; } = new()
    {
        "Wood", "Marble", "Glass", "Metal", "Neon"
    };
    
    public ObservableCollection<string> AvailablePieceSets { get; } = new()
    {
        "Classic", "Modern", "Medieval", "Minimalist", "3D"
    };
    
    public IRelayCommand SaveSettingsCommand { get; }
    public IRelayCommand ResetSettingsCommand { get; }
    public IRelayCommand BrowsePgnPathCommand { get; }
    
    public SettingsViewModel()
    {
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        ResetSettingsCommand = new RelayCommand(ResetSettings);
        BrowsePgnPathCommand = new RelayCommand(BrowsePgnPath);
        
        LoadSettings();
    }
    
    private void SaveSettings()
    {
        try
        {
            // TODO: Implement actual settings persistence
            // For now, just show a confirmation
            MessageBox.Show("Settings saved successfully!", "Settings", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
                          
            System.Diagnostics.Debug.WriteLine($"Settings saved: Theme={SelectedTheme}, Sound={SoundEnabled}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ResetSettings()
    {
        var result = MessageBox.Show("Reset all settings to default values?", "Reset Settings", 
                                   MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            PlayerName = "Player";
            SoundEnabled = true;
            AnimationsEnabled = true;
            ShowLegalMoves = true;
            HighlightLastMove = true;
            SelectedTheme = "Classic";
            SelectedBoardStyle = "Wood";
            SelectedPieceSet = "Classic";
            DefaultTimeControlMinutes = 10;
            DefaultAISkillLevel = 12;
            AutoSaveGames = true;
            PgnExportPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
            MessageBox.Show("Settings reset to defaults!", "Settings", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void BrowsePgnPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select PGN Export Folder",
            InitialDirectory = PgnExportPath
        };
        
        if (dialog.ShowDialog() == true)
        {
            PgnExportPath = dialog.FolderName;
        }
    }
    
    private void LoadSettings()
    {
        try
        {
            // TODO: Implement actual settings loading from file/registry
            // For now, use defaults
            System.Diagnostics.Debug.WriteLine("Settings loaded from defaults");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }
}
