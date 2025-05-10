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

        // ������������ App.axaml.cs
        public override void OnFrameworkInitializationCompleted()
        {
            // ������� DataAnnotations validator ��� ��������� ������������������
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // �������������� ������ ���
                var themeService = ThemeService.Instance;

                // �������������� ������ ����������� (��� �������)
                var notificationService = NotificationService.Instance;

                // ������� ��������� DbContext
                var dbContext = new PostgresContext();

                // ������� MainWindow � AuthPage � �������� ���������� ��������
                var mainViewModel = new MainWindowViewModel();

                // ������� ������� ���� � ������������� DataContext
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // ������������� AuthPage ��� ��������� �������������
                mainViewModel.CurrentView = new AuthPage(mainViewModel, dbContext);

                desktop.MainWindow = mainWindow;

                // ������������ ������� �������� ���������� ��� ���������� ��������� ��������
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