using System.Windows;
using LeagueClientTool.App.ViewModels;

namespace LeagueClientTool.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
