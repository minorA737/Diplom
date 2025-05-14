using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;  // ��� ������ ����� ��� AttachDevTools()
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using Task = ManufactPlanner.Models.Task;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class TaskEditDialog : Window
    {
        public TaskEditDialog()
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif

        }

        public TaskEditDialog(PostgresContext dbContext, Guid currentUserId, Task taskToEdit)
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif


            // ������������� ViewModel
            DataContext = new TaskEditDialogViewModel(dbContext, currentUserId, taskToEdit);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// ���������� ������ � ���������� ����������� ������ ��� null, ���� ������ ��� �������
        /// </summary>
        /// <param name="parent">������������ ����</param>
        /// <param name="dbContext">�������� ���� ������</param>
        /// <param name="currentUserId">������������� �������� ������������</param>
        /// <param name="taskToEdit">������ ��� ��������������</param>
        /// <returns>����������� ������ ��� null, ���� ������ ��� �������</returns>
        public static async Task<Task> ShowDialog(Window parent, PostgresContext dbContext, Guid currentUserId, Task taskToEdit)
        {
            var dialog = new TaskEditDialog(dbContext, currentUserId, taskToEdit);
            var viewModel = dialog.DataContext as TaskEditDialogViewModel;

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
            return result.Success ? result.Task : null;
        }
    }
}