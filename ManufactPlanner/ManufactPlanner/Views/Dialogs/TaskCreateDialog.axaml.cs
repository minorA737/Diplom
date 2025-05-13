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

            // Устанавливаем ViewModel
            DataContext = new TaskCreateDialogViewModel(dbContext, currentUserId);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Показывает диалог и возвращает созданную задачу или null, если диалог был отменен
        /// </summary>
        /// <param name="parent">Родительское окно</param>
        /// <param name="dbContext">Контекст базы данных</param>
        /// <param name="currentUserId">Идентификатор текущего пользователя</param>
        /// <returns>Созданная задача или null, если диалог был отменен</returns>
        public static async Task<Task> ShowDialog(Window parent, PostgresContext dbContext, Guid currentUserId)
        {
            // При каждом вызове создаем новый экземпляр диалогового окна
            var dialog = new TaskCreateDialog(dbContext, currentUserId);
            var viewModel = dialog.DataContext as TaskCreateDialogViewModel;

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

            // Освобождаем ресурсы ViewModel
            viewModel = null;

            return result.Success ? result.Task : null;
        }
    }
}