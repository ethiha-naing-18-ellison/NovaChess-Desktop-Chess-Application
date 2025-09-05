using System.Windows;
using System.Windows.Controls;
using NovaChess.App.ViewModels;

namespace NovaChess.App.Views;

public partial class PgnLibraryView : UserControl
{
    public PgnLibraryView()
    {
        InitializeComponent();
    }
    
    private void CopyPgnButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PgnLibraryViewModel viewModel && !string.IsNullOrEmpty(viewModel.SelectedPgnContent))
        {
            try
            {
                Clipboard.SetText(viewModel.SelectedPgnContent);
                MessageBox.Show("PGN copied to clipboard!", "Copy Successful", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying PGN: {ex.Message}", "Copy Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("No game selected or PGN content is empty.", "Nothing to Copy", 
                          MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
