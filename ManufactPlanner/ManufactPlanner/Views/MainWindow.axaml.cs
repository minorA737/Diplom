using Avalonia.Controls;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly PostgresContext _dbContext;

        public MainWindow()
        {
            InitializeComponent();

            //_dbContext = new PostgresContext();
            //_viewModel = new MainWindowViewModel();

            //// Создаем окно авторизации и показываем его вместо главного окна
            //var authViewModel = new AuthViewModel(_viewModel, _dbContext);
            //_viewModel.CurrentView = new AuthPage(authViewModel);

            //DataContext = _viewModel;
        }
    }
}