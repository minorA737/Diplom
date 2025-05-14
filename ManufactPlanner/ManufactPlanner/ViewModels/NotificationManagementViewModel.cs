using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ManufactPlanner.Views.Dialogs;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class NotificationManagementViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly NotificationService _notificationService;

        private string _searchText = string.Empty;
        private string _selectedType = "Все";
        private string _selectedStatus = "Все";
        private DateTimeOffset _startDate = DateTimeOffset.Now.AddDays(-30);
        private DateTimeOffset _endDate = DateTimeOffset.Now;
        private bool _isLoading = false;

        public ObservableCollection<string> NotificationTypes { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> NotificationStatuses { get; } = new ObservableCollection<string>();

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                // Автоматический поиск при изменении текста
                SearchCommand.Execute(null);
            }
        }

        public string SelectedType
        {
            get => _selectedType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedType, value);
                FilterCommand.Execute(null);
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStatus, value);
                FilterCommand.Execute(null);

            }
        }

        public DateTimeOffset StartDate
        {
            get => _startDate;
            set
            {
                this.RaiseAndSetIfChanged(ref _startDate, value);
                FilterCommand.Execute(null);
            }
        }

        public DateTimeOffset EndDate
        {
            get => _endDate;
            set
            {
                this.RaiseAndSetIfChanged(ref _endDate, value);
                FilterCommand.Execute(null);
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand SearchCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DeleteNotificationCommand { get; }
        public ICommand OpenNotificationCommand { get; }
        public ICommand MarkAsUnreadCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ClearSelectionCommand { get; }

        #endregion

        // Добавляем недостающие свойства для пагинации
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize = 50;
        private int _totalItems = 0;

        // Добавляем свойства для фильтров
        private string _searchUser = string.Empty;
        private string _selectedReadStatus = "Все";
        private string _selectedNotificationType = "Все типы";

        #region Properties

        // Добавляем недостающие свойства
        public string SearchUser
        {
            get => _searchUser;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchUser, value);
                FilterCommand.Execute(null);
            }
        }

        public string SelectedReadStatus
        {
            get => _selectedReadStatus;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedReadStatus, value);
                FilterCommand.Execute(null);
            }
        }

        public string SelectedNotificationType
        {
            get => _selectedNotificationType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedNotificationType, value);
                FilterCommand.Execute(null);
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => this.RaiseAndSetIfChanged(ref _totalPages, value);
        }

        public string PaginationInfo => $"Показано {(CurrentPage - 1) * _pageSize + 1}-{Math.Min(CurrentPage * _pageSize, _totalItems)} из {_totalItems} (Период: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy})";

        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        public bool IsEmpty => !AllNotifications.Any() && !IsLoading;

        // Коллекция для всех уведомлений (с пагинацией)
        public ObservableCollection<NotificationItemInfoViewModel> AllNotifications { get; } = new ObservableCollection<NotificationItemInfoViewModel>();

        #endregion

        #region Additional Commands

        public ICommand NavigateToItemCommand { get; }
        public ICommand MarkAsReadCommand { get; }
        public ICommand ShowStatisticsCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        #endregion

        public NotificationManagementViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _notificationService = NotificationService.Instance;

            // Инициализация команд
            SearchCommand = ReactiveCommand.CreateFromTask(ApplyFiltersAsync);
            FilterCommand = ReactiveCommand.CreateFromTask(ApplyFiltersAsync);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadNotificationsAsync);
            NavigateToItemCommand = ReactiveCommand.Create<NotificationItemInfoViewModel>(NavigateToItem);
            MarkAsReadCommand = ReactiveCommand.CreateFromTask<NotificationItemInfoViewModel>(MarkAsReadAsync);
            ShowStatisticsCommand = ReactiveCommand.CreateFromTask(ShowStatisticsAsync);
            PreviousPageCommand = ReactiveCommand.CreateFromTask(GoPreviousPage, this.WhenAnyValue(x => x.CanGoPrevious));
            NextPageCommand = ReactiveCommand.CreateFromTask(GoNextPage, this.WhenAnyValue(x => x.CanGoNext));

            // Инициализация фильтров
            InitializeFilters();

            // Загрузка уведомлений
            _ = LoadNotificationsAsync();
        }

        private async System.Threading.Tasks.Task LoadNotificationsAsync()
        {
            IsLoading = true;

            try
            {
                // Строим запрос с учетом фильтров
                var query = _dbContext.Notifications
                    .Include(n => n.User)
                    .AsQueryable();

                // Применяем фильтры
                query = ApplyFilters(query);

                // Подсчитываем общее количество
                _totalItems = await query.CountAsync();
                TotalPages = (int)Math.Ceiling((double)_totalItems / _pageSize);

                // Применяем пагинацию
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((CurrentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToListAsync();

                // Обновляем коллекцию
                AllNotifications.Clear();
                int index = 0;
                foreach (var notification in notifications)
                {
                    AllNotifications.Add(new NotificationItemInfoViewModel
                    {
                        Id = notification.Id,
                        Title = notification.Title,
                        Message = notification.Message,
                        TypeDisplayName = GetDisplayType(notification.NotificationType),
                        IsRead = notification.IsRead ?? false,
                        CreatedAt = notification.CreatedAt ?? DateTime.Now,
                        LinkTo = notification.LinkTo,
                        RecipientName = $"{notification.User?.FirstName} {notification.User?.LastName}".Trim(),
                        RecipientUsername = notification.User?.Username ?? "",
                        UserId = notification.UserId ?? Guid.Empty,
                        IsAlternate = index % 2 == 1,
                        TaskId = ExtractTaskIdFromLink(notification.LinkTo)
                    });
                    index++;
                }

                // Обновляем связанные свойства
                this.RaisePropertyChanged(nameof(PaginationInfo));
                this.RaisePropertyChanged(nameof(IsEmpty));
                this.RaisePropertyChanged(nameof(CanGoPrevious));
                this.RaisePropertyChanged(nameof(CanGoNext));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке уведомлений: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private string GetDisplayType(string notificationType)
        {
            return notificationType switch
            {
                "task_assigned" => "Новая задача",
                "status_changed" => "Изменение статуса",
                "new_comment" => "Новый комментарий",
                "deadline_approaching" => "Приближение дедлайна",
                "task_reassigned" => "Переназначение задачи",
                _ => "Другое"
            };
        }
        private IQueryable<Notification> ApplyFilters(IQueryable<Notification> query)
        {
            // Преобразуем DateTimeOffset в DateTime для сравнения с базой данных
            var startDateTime = StartDate.DateTime;
            var endDateTime = EndDate.DateTime.AddDays(1);

            // Фильтр по периоду
            query = query.Where(n => n.CreatedAt >= startDateTime && n.CreatedAt <= endDateTime);

            // Фильтр по статусу прочтения
            if (SelectedReadStatus == "Прочитанные")
            {
                query = query.Where(n => n.IsRead == true);
            }
            else if (SelectedReadStatus == "Непрочитанные")
            {
                query = query.Where(n => n.IsRead != true);
            }

            // Фильтр по типу уведомления
            if (SelectedNotificationType != "Все типы")
            {
                var notificationType = GetNotificationTypeByDisplayName(SelectedNotificationType);
                query = query.Where(n => n.NotificationType == notificationType);
            }

            // Фильтр по пользователю
            if (!string.IsNullOrWhiteSpace(SearchUser))
            {
                var searchLower = SearchUser.ToLower();
                query = query.Where(n => n.User.FirstName.ToLower().Contains(searchLower) ||
                                        n.User.LastName.ToLower().Contains(searchLower) ||
                                        n.User.Username.ToLower().Contains(searchLower));
            }

            return query;
        }

        private string GetNotificationTypeByDisplayName(string displayName)
        {
            return displayName switch
            {
                "Назначение задачи" => "task_assigned",
                "Изменение статуса" => "status_changed",
                "Новый комментарий" => "comment_added",
                "Приближение дедлайна" => "deadline_approaching",
                _ => ""
            };
        }

        private void NavigateToItem(NotificationItemInfoViewModel notification)
        {
            if (notification == null)
                return;

            // Отмечаем как прочитанное, если не прочитано
            if (!notification.IsRead)
            {
                _ = MarkAsReadAsync(notification);
            }

            // Переходим по ссылке
            if (notification.TaskId > 0)
            {
                _mainWindowViewModel.NavigateToTaskDetails(notification.TaskId);
            }
            else if (!string.IsNullOrEmpty(notification.LinkTo) && notification.LinkTo.StartsWith("/orders/"))
            {
                if (int.TryParse(notification.LinkTo.Substring(8), out int orderId))
                {
                    _mainWindowViewModel.NavigateToOrderDetails(orderId);
                }
            }
        }

        private async System.Threading.Tasks.Task MarkAsReadAsync(NotificationItemInfoViewModel notification)
        {
            if (notification == null || notification.IsRead)
                return;

            try
            {
                await _notificationService.MarkNotificationAsReadAsync(notification.Id);
                notification.IsRead = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отметке уведомления как прочитанного: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ShowStatisticsAsync()
        {
            try
            {
                IsLoading = true;

                var totalCount = await _dbContext.Notifications.CountAsync();
                var unreadCount = await _dbContext.Notifications.CountAsync(n => n.IsRead != true);
                var readCount = totalCount - unreadCount;

                // Получаем статистику по типам уведомлений
                var typeStats = await _dbContext.Notifications
                    .Where(n => n.CreatedAt >= StartDate.DateTime && n.CreatedAt <= EndDate.DateTime)
                    .GroupBy(n => n.NotificationType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                var message = $"Статистика уведомлений:\n\n" +
                              $"Всего: {totalCount}\n" +
                              $"Прочитано: {readCount} ({(totalCount > 0 ? (readCount * 100 / totalCount) : 0)}%)\n" +
                              $"Не прочитано: {unreadCount}\n\n" +
                              $"По типам (за выбранный период):\n" +
                              string.Join("\n", typeStats.Select(s => $"• {GetDisplayType(s.Type)}: {s.Count}"));

                // Получаем родительское окно
                var parent = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (parent != null)
                {
                    await MessageBoxDialog.ShowDialog(parent, "Статистика уведомлений", message, "Закрыть");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при показе статистики: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task GoPreviousPage()
        {
            if (CanGoPrevious)
            {
                CurrentPage--;
                await LoadNotificationsAsync();
            }
        }

        private async System.Threading.Tasks.Task GoNextPage()
        {
            if (CanGoNext)
            {
                CurrentPage++;
                await LoadNotificationsAsync();
            }
        }

        private async System.Threading.Tasks.Task ApplyFiltersAsync()
        {
            CurrentPage = 1; // Сбрасываем на первую страницу при изменении фильтров
            await LoadNotificationsAsync();
        }

        private void InitializeFilters()
        {
            // Инициализация типов уведомлений для фильтра
            var notificationTypes = new[]
            {
            "Все типы",
            "Назначение задачи",
            "Изменение статуса",
            "Новый комментарий",
            "Приближение дедлайна"
        };

            // В реальном приложении эти данные могут храниться в ресурсах или конфигурации
        }

        private int ExtractTaskIdFromLink(string link)
        {
            if (string.IsNullOrEmpty(link) || !link.StartsWith("/tasks/"))
                return 0;

            if (int.TryParse(link.Substring(7), out int taskId))
                return taskId;

            return 0;
        }
    }

    // Обновленная модель для отображения информации о уведомлениях
    public class NotificationItemInfoViewModel : ViewModelBase
    {
        private bool _isRead;

        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TypeDisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string LinkTo { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientUsername { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public bool IsAlternate { get; set; }
        public int TaskId { get; set; }

        public bool IsRead
        {
            get => _isRead;
            set => this.RaiseAndSetIfChanged(ref _isRead, value);
        }

        // Форматированные свойства для отображения
        public string FormattedCreatedDate => CreatedAt.ToString("dd.MM.yyyy");
        public string FormattedCreatedTime => CreatedAt.ToString("HH:mm");
        public string FormattedReadDate => IsRead ? "Прочитано" : "";
        public string FormattedReadTime => IsRead ? "" : "";
    }
}