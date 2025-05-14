using System;
using ManufactPlanner.ViewModels;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using System.Diagnostics;
using Avalonia.Platform;

namespace ManufactPlanner.Services
{
    public class TrayService : IDisposable
    {
        private MainWindowViewModel _mainViewModel;
        private bool _disposed = false;
        private bool _isInTray = false;

        public TrayService(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            Debug.WriteLine("TrayService создан");
        }

        public void ShowInTray()
        {
            try
            {
                Debug.WriteLine("Попытка скрыть приложение в трей...");

                var desktopApp = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                if (desktopApp?.MainWindow != null)
                {
                    var mainWindow = desktopApp.MainWindow;

                    Debug.WriteLine($"Скрываем окно. Текущее состояние - Visible: {mainWindow.IsVisible}");

                    // Скрываем окно
                    mainWindow.Hide();
                    _isInTray = true;

                    Debug.WriteLine("Окно скрыто. Показываем уведомление...");

                    // Показываем уведомление о том, что приложение работает в фоне
                    ShowSystemNotification("ManufactPlanner", "Приложение работает в фоновом режиме");

                    Debug.WriteLine("Приложение успешно скрыто в трей");
                }
                else
                {
                    Debug.WriteLine("Не удалось получить MainWindow для сокрытия в трей");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сокрытия в трей: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        public void HideFromTray()
        {
            Debug.WriteLine("Скрываем из трея...");
            _isInTray = false;
        }

        public void ShowTrayNotification(string title, string text)
        {
            try
            {
                Debug.WriteLine($"Показываем trau уведомление: {title} - {text}");
                ShowSystemNotification(title, text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка показа уведомления: {ex.Message}");
            }
        }

        private void ShowSystemNotification(string title, string text)
        {
            try
            {
                Debug.WriteLine($"Создаем системное уведомление: {title} - {text}");

                // Используем простое всплывающее уведомление
                Dispatcher.UIThread.Post(() =>
                {
                    // Создаем собственное окно уведомления
                    NotificationWindowService.ShowNotification(title, text);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка показа системного уведомления: {ex.Message}");
            }
        }

        public bool IsInTray => _isInTray;

        public void Dispose()
        {
            if (!_disposed)
            {
                Debug.WriteLine("Освобождаем ресурсы TrayService");
                _disposed = true;
            }
        }
    }
}