// Views/Dialogs/OrderEditDialog.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.ViewModels.Dialogs;
using ManufactPlanner.Models;
using System.Threading.Tasks;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class OrderEditDialog : Window
    {
        public OrderEditDialog()
        {
            InitializeComponent();
        }

        // Конструктор для создания нового заказа
        public OrderEditDialog(PostgresContext dbContext)
        {
            InitializeComponent();
            var viewModel = new OrderEditDialogViewModel(dbContext);
            DataContext = viewModel;
            viewModel.SetDialogWindow(this);
        }

        // Конструктор для редактирования существующего заказа
        public OrderEditDialog(PostgresContext dbContext, int orderId)
        {
            InitializeComponent();
            var viewModel = new OrderEditDialogViewModel(dbContext, orderId);
            DataContext = viewModel;
            viewModel.SetDialogWindow(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Метод для открытия диалога и получения результата
        public static async Task<bool> ShowDialogAsync(Window owner, PostgresContext dbContext)
        {
            var dialog = new OrderEditDialog(dbContext);
            return await dialog.ShowDialog<bool>(owner);
        }

        // Метод для открытия диалога редактирования и получения результата
        public static async Task<bool> ShowDialogAsync(Window owner, PostgresContext dbContext, int orderId)
        {
            var dialog = new OrderEditDialog(dbContext, orderId);
            return await dialog.ShowDialog<bool>(owner);
        }
    }
}