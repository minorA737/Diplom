using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Views;
using Avalonia.Data.Core.Plugins;
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using Microsoft.Extensions.DependencyInjection;
using Splat;
using System;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ManufactPlanner
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Debug.WriteLine("Инициализация приложения...");

            // Удаляем DataAnnotations validator для улучшения производительности
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Инициализируем сервис тем
                var themeService = ThemeService.Instance;

                // Инициализируем сервис уведомлений (без запуска)
                var notificationService = NotificationService.Instance;

                // Создаем экземпляр DbContext
                var dbContext = new PostgresContext();

                // Создаем MainWindow с AuthPage в качестве начального контента
                var mainViewModel = new MainWindowViewModel();

                // Устанавливаем ссылку на MainWindow в AppWindows
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // Важно: устанавливаем ссылку на главное окно в mainViewModel
                mainViewModel.MainWindow = mainWindow;

                // Устанавливаем AuthPage как начальное представление
                mainViewModel.CurrentView = new AuthPage(mainViewModel, dbContext);

                desktop.MainWindow = mainWindow;
                AppWindows.MainWindow = mainWindow;

                Debug.WriteLine("Главное окно создано и настроено");

                // Обрабатываем событие закрытия приложения для корректной остановки сервисов
                desktop.ShutdownRequested += (sender, e) =>
                {
                    Debug.WriteLine("Получен запрос на завершение работы приложения");

                    // Только если это принудительное закрытие
                    var mainViewModel = desktop.MainWindow?.DataContext as MainWindowViewModel;
                    if (mainViewModel != null && !mainViewModel._forceClose)
                    {
                        Debug.WriteLine("Отменяем закрытие приложения (оно будет скрыто в трей)");
                        e.Cancel = true;
                        return;
                    }

                    Debug.WriteLine("Принудительное закрытие приложения...");
                    notificationService.Stop();
                    notificationService.Dispose();
                };

                // Добавляем обработчик события Closing для MainWindow
                mainWindow.Closing += (sender, e) =>
                {
                    Debug.WriteLine("Попытка закрыть главное окно");
                    // Это событие будет обработано в MainWindow.axaml.cs
                };
            }

            base.OnFrameworkInitializationCompleted();
            Debug.WriteLine("Инициализация приложения завершена");
        }
    }
}