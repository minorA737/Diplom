using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManufactPlanner.Services
{
    public static class NotificationDialogService
    {
        private static readonly Dictionary<int, DesktopNotificationDialog> _activeDialogs = new Dictionary<int, DesktopNotificationDialog>();

        public static void ShowNotificationDialog(NotificationViewModel notification, MainWindowViewModel mainWindowViewModel, NotificationService notificationService)
        {
            // Проверяем, включены ли десктопные уведомления для текущего пользователя
            if (!ShouldShowDesktopNotification(mainWindowViewModel, notificationService))
            {
                return;
            }

            // Проверяем, не отображается ли уже диалоговое окно для этого уведомления
            if (_activeDialogs.ContainsKey(notification.Id))
            {
                // Окно уже отображается, активируем его
                _activeDialogs[notification.Id].Activate();
                return;
            }

            // Создаем новое диалоговое окно
            var dialog = new DesktopNotificationDialog(notification, mainWindowViewModel, notificationService);

            // Регистрируем окно в словаре активных окон
            _activeDialogs[notification.Id] = dialog;

            // Обработчик закрытия окна для удаления его из словаря
            dialog.Closed += (sender, args) =>
            {
                _activeDialogs.Remove(notification.Id);
            };

            // Отображаем окно
            dialog.Show();
        }

        private static bool ShouldShowDesktopNotification(MainWindowViewModel mainWindowViewModel, NotificationService notificationService)
        {
            // Логика определения, нужно ли показывать десктопные уведомления
            // Используем ту же логику, что и в NotificationService
            // Но здесь мы можем использовать кешированное значение из контекста приложения

            // По умолчанию показываем, если нет конкретных настроек
            return mainWindowViewModel?.NotifyDesktopEnabled ?? true;
        }

        public static void CloseAllDialogs()
        {
            foreach (var dialog in _activeDialogs.Values)
            {
                dialog.Close();
            }

            _activeDialogs.Clear();
        }
    }
}