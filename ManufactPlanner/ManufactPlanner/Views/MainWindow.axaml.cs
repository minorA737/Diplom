using Avalonia.Controls;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Views.Dialogs;
using System.ComponentModel;

namespace ManufactPlanner.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppWindows.MainWindow = this;

            // Обрабатываем событие закрытия окна
            Closing += OnClosing;
        }

        private async void OnClosing(object sender, CancelEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel == null)
                return;

            e.Cancel = true; // Отменяем закрытие по умолчанию

            // Показываем диалог выбора
            var result = await MessageBoxDialog.ShowDialog(
                this,
                "Закрытие приложения",
                "Вы хотите закрыть приложение полностью или оставить его работать в фоновом режиме?",
                "Закрыть полностью",
                "В фоновом режиме"
            );

            if (result)
            {
                // Пользователь выбрал полное закрытие
                viewModel.ForceExit();
            }
            else
            {
                // Пользователь выбрал работу в фоне
                viewModel.HideToTray();
            }
        }
    }
}