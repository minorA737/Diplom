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
            Debug.WriteLine("������������� ����������...");

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

                // ������������� ������ �� MainWindow � AppWindows
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // �����: ������������� ������ �� ������� ���� � mainViewModel
                mainViewModel.MainWindow = mainWindow;

                // ������������� AuthPage ��� ��������� �������������
                mainViewModel.CurrentView = new AuthPage(mainViewModel, dbContext);

                desktop.MainWindow = mainWindow;
                AppWindows.MainWindow = mainWindow;

                Debug.WriteLine("������� ���� ������� � ���������");

                // ������������ ������� �������� ���������� ��� ���������� ��������� ��������
                desktop.ShutdownRequested += (sender, e) =>
                {
                    Debug.WriteLine("������� ������ �� ���������� ������ ����������");

                    // ������ ���� ��� �������������� ��������
                    var mainViewModel = desktop.MainWindow?.DataContext as MainWindowViewModel;
                    if (mainViewModel != null && !mainViewModel._forceClose)
                    {
                        Debug.WriteLine("�������� �������� ���������� (��� ����� ������ � ����)");
                        e.Cancel = true;
                        return;
                    }

                    Debug.WriteLine("�������������� �������� ����������...");
                    notificationService.Stop();
                    notificationService.Dispose();
                };

                // ��������� ���������� ������� Closing ��� MainWindow
                mainWindow.Closing += (sender, e) =>
                {
                    Debug.WriteLine("������� ������� ������� ����");
                    // ��� ������� ����� ���������� � MainWindow.axaml.cs
                };
            }

            base.OnFrameworkInitializationCompleted();
            Debug.WriteLine("������������� ���������� ���������");
        }
    }
}