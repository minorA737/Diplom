// Views/Dialogs/OrderPositionDialog.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.ViewModels.Dialogs;
using ManufactPlanner.Models;
using System.Threading.Tasks;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class OrderPositionDialog : Window
    {
        public OrderPositionDialog()
        {
            InitializeComponent();
        }

        // ����������� ��� �������� ����� �������
        public OrderPositionDialog(PostgresContext dbContext, int orderId, bool isNew = true)
        {
            InitializeComponent();
            var viewModel = new OrderPositionDialogViewModel(dbContext, orderId, isNew);
            DataContext = viewModel;
            viewModel.SetDialogWindow(this);
        }

        // ����������� ��� �������������� ������������ �������
        public OrderPositionDialog(PostgresContext dbContext, int positionId)
        {
            InitializeComponent();
            var viewModel = new OrderPositionDialogViewModel(dbContext, positionId);
            DataContext = viewModel;
            viewModel.SetDialogWindow(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // ����� ��� �������� ������� �������� ����� ������� � ��������� ����������
        public static async Task<bool> ShowDialogAsync(Window owner, PostgresContext dbContext, int orderId)
        {
            var dialog = new OrderPositionDialog(dbContext, orderId, true);
            return await dialog.ShowDialog<bool>(owner);
        }

        // ����� ��� �������� ������� �������������� ������� � ��������� ����������
        public static async Task<bool> ShowEditDialogAsync(Window owner, PostgresContext dbContext, int positionId)
        {
            var dialog = new OrderPositionDialog(dbContext, positionId);
            return await dialog.ShowDialog<bool>(owner);
        }
    }
}