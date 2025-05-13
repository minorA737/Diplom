using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using Task = ManufactPlanner.Models.Task;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class TaskCreateDialog : Window
    {

        public TaskCreateDialog(PostgresContext dbContext, Guid currentUserId)
        {
            InitializeComponent();
            this.AttachDevTools();

            // ������������� ViewModel
            DataContext = new TaskCreateDialogViewModel(dbContext, currentUserId);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// ���������� ������ � ���������� ��������� ������ ��� null, ���� ������ ��� �������
        /// </summary>
        /// <param name="parent">������������ ����</param>
        /// <param name="dbContext">�������� ���� ������</param>
        /// <param name="currentUserId">������������� �������� ������������</param>
        /// <returns>��������� ������ ��� null, ���� ������ ��� �������</returns>
        public static async Task<Task> ShowDialog(Window parent, PostgresContext dbContext, Guid currentUserId)
        {
            // ��� ������ ������ ������� ����� ��������� ����������� ����
            var dialog = new TaskCreateDialog(dbContext, currentUserId);
            var viewModel = dialog.DataContext as TaskCreateDialogViewModel;

            // ����������� ����������� ��� ������ �������
            var tcs = new TaskCompletionSource<(bool Success, Task Task)>();

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

            // ����������� ������� ViewModel
            viewModel = null;

            return result.Success ? result.Task : null;
        }
    }
}