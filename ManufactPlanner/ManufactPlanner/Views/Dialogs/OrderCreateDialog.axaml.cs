using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;  // ��� ������ ����� ��� AttachDevTools()
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using ReactiveUI;
using System;
using System.Threading.Tasks;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class OrderCreateDialog : Window
    {
        public OrderCreateDialog()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public OrderCreateDialog(PostgresContext dbContext)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            // ������������� ViewModel
            DataContext = new OrderCreateDialogViewModel(dbContext);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// ���������� ������ � ���������� ��������� ����� ��� null, ���� ������ ��� �������
        /// </summary>
        /// <param name="parent">������������ ����</param>
        /// <param name="dbContext">�������� ���� ������</param>
        /// <returns>��������� ����� ��� null, ���� ������ ��� �������</returns>
        public static async Task<Order> ShowDialog(Window parent, PostgresContext dbContext)
        {
            var dialog = new OrderCreateDialog(dbContext);
            var viewModel = dialog.DataContext as OrderCreateDialogViewModel;

            // ����������� ����������� ��� ������ �������
            var tcs = new TaskCompletionSource<(bool Success, Order Order)>();

            // ������������� �� �������
            var saveDisposable = viewModel.SaveCommand.Subscribe(result =>
            {
                tcs.SetResult(result);
                dialog.Close();
            });

            var cancelDisposable = viewModel.CancelCommand.Subscribe(_ =>
            {
                tcs.SetResult((false, null));
                dialog.Close();
            });

            // ���������� ������
            await dialog.ShowDialog(parent);

            // ������������ �� ������
            saveDisposable.Dispose();
            cancelDisposable.Dispose();

            // ������� ���������
            var result = await tcs.Task;
            return result.Success ? result.Order : null;

        }
    }
}