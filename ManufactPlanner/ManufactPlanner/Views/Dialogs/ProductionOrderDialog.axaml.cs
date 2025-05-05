using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.ViewModels.Dialogs;
using ManufactPlanner.Models;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class ProductionOrderDialog : Window
    {
        public ProductionOrderDialog()
        {
            InitializeComponent();
        }

        public ProductionOrderDialog(PostgresContext dbContext)
        {
            InitializeComponent();
            var viewModel = new ProductionOrderDialogViewModel(dbContext);
            DataContext = viewModel;

            // Устанавливаем ссылку на окно в ViewModel
            viewModel.SetDialogWindow(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}