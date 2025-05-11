using Avalonia.Controls;
using ManufactPlanner.Services;
using ManufactPlanner.Views;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class NotificationDialogViewModel : ViewModelBase
    {
        private readonly Window _window;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly NotificationService _notificationService;
        private readonly NotificationViewModel _notification;
        private static System.Threading.Timer _postponeTimer;

        public NotificationViewModel Notification => _notification;

        public ICommand ReadCommand { get; }
        public ICommand PostponeCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand CloseCommand { get; }

        public NotificationDialogViewModel(NotificationViewModel notification, MainWindowViewModel mainWindowViewModel, NotificationService notificationService, Window window)
        {
            _notification = notification;
            _mainWindowViewModel = mainWindowViewModel;
            _notificationService = notificationService;
            _window = window;

            ReadCommand = ReactiveCommand.CreateFromTask(MarkAsReadAsync);
            PostponeCommand = ReactiveCommand.Create(PostponeNotification);
            OpenCommand = ReactiveCommand.CreateFromTask(OpenNotificationsAsync);
            CloseCommand = ReactiveCommand.Create(PostponeNotification);
        }

        private async Task MarkAsReadAsync()
        {
            if (_notification.Id > 0)
            {
                await _notificationService.MarkNotificationAsReadAsync(_notification.Id);
            }

            _window.Close();

            // Обновляем счетчик уведомлений в UI
            await _notificationService.UpdateUnreadCountAsync(_mainWindowViewModel.CurrentUserId);
        }

        private void PostponeNotification()
        {
            _window.Close();

            // Отменяем существующий таймер, если он есть
            _postponeTimer?.Dispose();

            // Создаем новый таймер для повторного показа уведомления через 10 минут
            _postponeTimer = new System.Threading.Timer(async (state) =>
            {
                var notification = state as NotificationViewModel;
                if (notification != null)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        NotificationDialogService.ShowNotificationDialog(notification, _mainWindowViewModel, _notificationService);
                    });
                }
            }, _notification, TimeSpan.FromMinutes(1), System.Threading.Timeout.InfiniteTimeSpan);
        }

        private async Task OpenNotificationsAsync()
        {
            // Отмечаем уведомление как прочитанное
            if (_notification.Id > 0)
            {
                await _notificationService.MarkNotificationAsReadAsync(_notification.Id);
            }

            // Переходим на страницу уведомлений
            _mainWindowViewModel.NavigateToNotifications();

            // Закрываем диалоговое окно
            _window.Close();
        }
    }
}