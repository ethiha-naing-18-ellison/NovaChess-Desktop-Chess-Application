using System.Windows;
using NovaChess.App.ViewModels;

namespace NovaChess.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
