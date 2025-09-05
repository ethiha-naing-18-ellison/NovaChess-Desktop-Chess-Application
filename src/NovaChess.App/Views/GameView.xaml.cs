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
            // Initialize the board control with the current board state
            if (viewModel.Board != null)
            {
                BoardControl.SetBoard(viewModel.Board);
            }
            
            // Subscribe to board changes
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(viewModel.Board) && viewModel.Board != null)
                {
                    BoardControl.SetBoard(viewModel.Board);
                }
            };
        }
    }
    
    private async void OnMoveMade(object? sender, MoveMadeEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"GameView.OnMoveMade called with move: {e.Move}");
        System.Diagnostics.Debug.WriteLine($"Move from: {e.Move.From}, to: {e.Move.To}, piece: {e.Move.Piece}");
        
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
