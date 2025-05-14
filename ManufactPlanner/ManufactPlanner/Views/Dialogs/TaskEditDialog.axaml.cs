using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;  // Эта строка нужна для AttachDevTools()
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


            // Устанавливаем ViewModel
            DataContext = new TaskEditDialogViewModel(dbContext, currentUserId, taskToEdit);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Показывает диалог и возвращает обновленную задачу или null, если диалог был отменен
        /// </summary>
        /// <param name="parent">Родительское окно</param>
        /// <param name="dbContext">Контекст базы данных</param>
        /// <param name="currentUserId">Идентификатор текущего пользователя</param>
        /// <param name="taskToEdit">Задача для редактирования</param>
        /// <returns>Обновленная задача или null, если диалог был отменен</returns>
        public static async Task<Task> ShowDialog(Window parent, PostgresContext dbContext, Guid currentUserId, Task taskToEdit)
        {
            var dialog = new TaskEditDialog(dbContext, currentUserId, taskToEdit);
            var viewModel = dialog.DataContext as TaskEditDialogViewModel;

            // Настраиваем обработчики для кнопок диалога
            var tcs = new TaskCompletionSource<(bool Success, Task Task)>();

            // Подписываемся на команды
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

            // Показываем диалог
            await dialog.ShowDialog(parent);

            // Отписываемся от команд
            saveDisposable.Dispose();
            cancelDisposable.Dispose();

            // Ожидаем результат
            var result = await tcs.Task;
            return result.Success ? result.Task : null;
        }
    }
}