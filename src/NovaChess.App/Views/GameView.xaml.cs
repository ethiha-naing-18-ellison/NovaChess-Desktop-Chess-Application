using System.Windows.Controls;
using NovaChess.App.Controls;
using System.Windows;

namespace NovaChess.App.Views;

public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();
        
        // Wire up the BoardControl move event
        BoardControl.MoveMade += OnMoveMade;
        
        // Subscribe to DataContext changes to initialize the board
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ViewModels.GameViewModel viewModel)
        {
            // CRITICAL: Initialize with new chess engine first
            if (viewModel.GameState != null)
            {
                BoardControl.SetGameState(viewModel.GameState, viewModel);
                System.Diagnostics.Debug.WriteLine("=== BOARDCONTROL INITIALIZED WITH GAMESTATE ===");
            }
            else
            {
                // Initialize new game if no GameState
                viewModel.NewGame();
                if (viewModel.GameState != null)
                {
                    BoardControl.SetGameState(viewModel.GameState, viewModel);
                    System.Diagnostics.Debug.WriteLine("=== BOARDCONTROL INITIALIZED WITH NEW GAMESTATE ===");
                }
            }
            
            // Fallback to old Board system
            if (viewModel.Board != null)
            {
                BoardControl.SetBoard(viewModel.Board);
            }
            
            // Subscribe to board and game state changes
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(viewModel.Board) && viewModel.Board != null)
                {
                    BoardControl.SetBoard(viewModel.Board);
                }
                else if (args.PropertyName == nameof(viewModel.GameState) && viewModel.GameState != null)
                {
                    // NEW: Use the new GameState with proper chess engine
                    BoardControl.SetGameState(viewModel.GameState, viewModel);
                    System.Diagnostics.Debug.WriteLine("Updated BoardControl with new GameState");
                }
            };
            
            // Initialize with current GameState if available
            if (viewModel.GameState != null)
            {
                BoardControl.SetGameState(viewModel.GameState, viewModel);
            }
            
            // IMPORTANT: Set DataContext so BoardControl can auto-detect ViewModel
            BoardControl.DataContext = viewModel;
        }
    }
    
    private async void OnMoveMade(object? sender, MoveMadeEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"GameView.OnMoveMade called with move: {e.Move}");
        System.Diagnostics.Debug.WriteLine($"Move from: {e.Move.From}, to: {e.Move.To}");
        
        if (DataContext is ViewModels.GameViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine("Calling viewModel.MakeMoveAsync");
            await viewModel.MakeMoveAsync(e.Move);
            System.Diagnostics.Debug.WriteLine("MakeMoveAsync completed");
            
            // Update the board control with the new board state
            if (viewModel.Board != null)
            {
                System.Diagnostics.Debug.WriteLine("Updating board control with new board state");
                BoardControl.SetBoard(viewModel.Board);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("DataContext is not GameViewModel");
        }
    }
}
