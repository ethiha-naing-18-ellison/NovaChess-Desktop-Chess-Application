using System;
using System.Windows;
using NovaChess.Core;
using NovaChess.App.Models;
using NovaChess.App.ViewModels;

namespace NovaChess.App.Views;

public partial class NewGameDialog : Window
{
    public GameConfiguration GameConfig { get; private set; }

    public NewGameDialog()
    {
        InitializeComponent();
        GameConfig = new GameConfiguration();
        
        // Wire up event handlers
        PvCRadio.Checked += OnGameModeChanged;
        PvPRadio.Checked += OnGameModeChanged;
        
        SkillSlider.ValueChanged += (s, e) => SkillValueText.Text = ((int)e.NewValue).ToString();
        DepthSlider.ValueChanged += (s, e) => DepthValueText.Text = ((int)e.NewValue).ToString();
        
        // Set default values
        GameConfig.GameMode = GameMode.PlayerVsPlayer;
        GameConfig.TimeControl = GameTimeControl.NoLimit;
        GameConfig.StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    }

    private void OnGameModeChanged(object sender, RoutedEventArgs e)
    {
        if (PvCRadio.IsChecked == true)
        {
            AISettingsGroup.Visibility = Visibility.Visible;
            GameConfig.GameMode = GameMode.PlayerVsComputer;
        }
        else
        {
            AISettingsGroup.Visibility = Visibility.Collapsed;
            GameConfig.GameMode = GameMode.PlayerVsPlayer;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get time control
            if (BlitzRadio.IsChecked == true)
                GameConfig.TimeControl = GameTimeControl.Blitz;
            else if (RapidRadio.IsChecked == true)
                GameConfig.TimeControl = GameTimeControl.Rapid;
            else if (ClassicalRadio.IsChecked == true)
                GameConfig.TimeControl = GameTimeControl.Classical;
            else if (CustomTimeRadio.IsChecked == true)
            {
                if (int.TryParse(CustomMinutesTextBox.Text, out int minutes))
                    GameConfig.TimeControl = GameTimeControl.Custom;
                else
                {
                    MessageBox.Show("Please enter a valid number of minutes.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
                GameConfig.TimeControl = GameTimeControl.NoLimit;

            // Get AI settings
            if (GameConfig.GameMode == GameMode.PlayerVsComputer)
            {
                GameConfig.AISkillLevel = (int)SkillSlider.Value;
                GameConfig.AISearchDepth = (int)DepthSlider.Value;
            }

            // Get starting position
            if (CustomPositionRadio.IsChecked == true)
            {
                GameConfig.StartingPosition = CustomFenTextBox.Text.Trim();
                // Validate FEN
                if (!IsValidFen(GameConfig.StartingPosition))
                {
                    MessageBox.Show("Please enter a valid FEN position.", "Invalid FEN", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            DialogResult = true;
            
            // Trigger game start and navigation
            StartNewGameWithConfig();
            
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting new game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void StartNewGameWithConfig()
    {
        try
        {
            // Get the GameViewModel and start a new game with the configuration
            var gameViewModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<GameViewModel>(App.Services);
            
            gameViewModel.InitializeNewGame(GameConfig);
            
            var modeText = GameConfig.GameMode == GameMode.PlayerVsComputer ? 
                $"Player vs Computer (Skill: {GameConfig.AISkillLevel})" : "Player vs Player";
                
            System.Diagnostics.Debug.WriteLine($"🎮 Dialog started new game: {modeText}");
            
            // Navigate to game view using the main window's DataContext
            if (Application.Current.MainWindow?.DataContext is MainWindowViewModel mainViewModel)
            {
                System.Diagnostics.Debug.WriteLine("🔄 Dialog navigating to Game view...");
                mainViewModel.NavigateTo("Game");
                System.Diagnostics.Debug.WriteLine("✅ Dialog navigation completed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Dialog could not find MainWindowViewModel");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Dialog game start failed: {ex.Message}");
        }
    }

    private bool IsValidFen(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen))
            return false;

        var parts = fen.Split(' ');
        if (parts.Length < 4)
            return false;

        // Basic validation - check if it contains valid characters
        var boardPart = parts[0];
        foreach (char c in boardPart)
        {
            if (!char.IsLetterOrDigit(c) && c != '/')
                return false;
        }

        return true;
    }
}
