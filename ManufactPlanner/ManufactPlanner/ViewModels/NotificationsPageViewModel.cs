// NotificationsPageViewModel.cs
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class NotificationsPageViewModel : ViewModelBase, IDisposable
    {
        private readonly NotificationService _notificationService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _isLoading = false;

        public ObservableCollection<NotificationViewModel> Notifications { get; } = new ObservableCollection<NotificationViewModel>();

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public ICommand OpenItemCommand { get; }
        public ICommand MarkAsReadCommand { get; }
        public ICommand MarkAllAsReadCommand { get; }
        public ICommand RefreshCommand { get; }

        public NotificationsPageViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _notificationService = NotificationService.Instance;

            // Команды
            OpenItemCommand = ReactiveCommand.Create<NotificationViewModel>(OpenNotificationItem);
            MarkAsReadCommand = ReactiveCommand.CreateFromTask<NotificationViewModel>(MarkAsReadAsync);
            MarkAllAsReadCommand = ReactiveCommand.CreateFromTask(MarkAllAsReadAsync);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadNotificationsAsync);

            // Подписка на новые уведомления
            _notificationService.NewNotifications
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(notification =>
                {
                    // Добавляем уведомление в начало списка
                    Notifications.Insert(0, notification);
                })
                .DisposeWith(_disposables);

            // Загружаем существующие уведомления
            LoadNotificationsAsync().ConfigureAwait(false);
        }

        private async System.Threading.Tasks.Task LoadNotificationsAsync()
        {
            if (_mainWindowViewModel.CurrentUserId == Guid.Empty)
                return;

            IsLoading = true;

            try
            {
                var notifications = await _notificationService.GetAllNotificationsAsync(_mainWindowViewModel.CurrentUserId);

                Notifications.Clear();
                foreach (var notification in notifications)
                {
                    Notifications.Add(notification);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenNotificationItem(NotificationViewModel notification)
        {
            if (!notification.IsRead)
            {
                // Отмечаем уведомление как прочитанное
                _notificationService.MarkNotificationAsReadAsync(notification.Id).ConfigureAwait(false);
                notification.IsRead = true;
            }

            // Переходим на страницу с задачей
            if (notification.TaskId > 0)
            {
                _mainWindowViewModel.NavigateToTaskDetails(notification.TaskId);
            }
            else if (!string.IsNullOrEmpty(notification.LinkTo))
            {
                // Можно добавить другие типы навигации в зависимости от LinkTo
                // Например, если linkTo содержит "orders/123", можно извлечь ID и перейти к заказу
            }
        }

        private async System.Threading.Tasks.Task MarkAsReadAsync(NotificationViewModel notification)
        {
            await _notificationService.MarkNotificationAsReadAsync(notification.Id);
            notification.IsRead = true;

            // Обновляем счетчик непрочитанных уведомлений
            await _notificationService.UpdateUnreadCountAsync(_mainWindowViewModel.CurrentUserId);
        }

        private async System.Threading.Tasks.Task MarkAllAsReadAsync()
        {
            if (_mainWindowViewModel.CurrentUserId == Guid.Empty)
                return;

            await _notificationService.MarkAllNotificationsAsReadAsync(_mainWindowViewModel.CurrentUserId);

            // Обновляем статус всех уведомлений в коллекции
            foreach (var notification in Notifications)
            {
                notification.IsRead = true;
            }

            // Обновляем счетчик непрочитанных уведомлений (должен стать 0)
            _mainWindowViewModel.UnreadNotificationsCount = 0;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}