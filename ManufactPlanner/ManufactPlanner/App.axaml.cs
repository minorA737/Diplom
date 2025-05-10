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

namespace ManufactPlanner
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Модифицируем App.axaml.cs
        public override void OnFrameworkInitializationCompleted()
        {
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

                // Создаем главное окно и устанавливаем DataContext
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // Устанавливаем AuthPage как начальное представление
                mainViewModel.CurrentView = new AuthPage(mainViewModel, dbContext);

                desktop.MainWindow = mainWindow;

                // Обрабатываем событие закрытия приложения для корректной остановки сервисов
                desktop.ShutdownRequested += (sender, e) =>
                {
                    notificationService.Stop();
                    notificationService.Dispose();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}