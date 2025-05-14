using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;  // Эта строка нужна для AttachDevTools()
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
            // Устанавливаем ViewModel
            DataContext = new OrderCreateDialogViewModel(dbContext);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Показывает диалог и возвращает созданный заказ или null, если диалог был отменен
        /// </summary>
        /// <param name="parent">Родительское окно</param>
        /// <param name="dbContext">Контекст базы данных</param>
        /// <returns>Созданный заказ или null, если диалог был отменен</returns>
        public static async Task<Order> ShowDialog(Window parent, PostgresContext dbContext)
        {
            var dialog = new OrderCreateDialog(dbContext);
            var viewModel = dialog.DataContext as OrderCreateDialogViewModel;

            // Настраиваем обработчики для кнопок диалога
            var tcs = new TaskCompletionSource<(bool Success, Order Order)>();

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
            return result.Success ? result.Order : null;

        }
    }
}