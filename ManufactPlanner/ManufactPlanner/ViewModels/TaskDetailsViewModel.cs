using System;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ManufactPlanner.Views.Dialogs;
using Avalonia.Controls;

namespace ManufactPlanner.ViewModels
{
    public class TaskDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly Window _parentWindow;
        private readonly Guid _currentUserId;

        // Свойства для отображения данных
        private string _taskId = string.Empty;
        private int _taskDbId;
        private string _taskName = string.Empty;
        private string _createdDate = string.Empty;
        private string _status = string.Empty;
        private string _statusColor = "#FFB74D";
        private string _priority = string.Empty;
        private string _priorityColor = "#FF7043";
        private string _deadline = string.Empty;
        private string _description = string.Empty;
        private string _assignee = string.Empty;
        private string _orderPosition = string.Empty;
        private string _stageStatus = string.Empty;
        private string _notes = string.Empty;
        private string _customer = string.Empty;
        private string _orderDeadline = string.Empty;
        private string _orderStatus = string.Empty;
        private bool _isLoading = true;
        private int _selectedTabIndex = 0;

        // Коллекции для вкладок
        private ObservableCollection<RelatedTaskViewModel> _relatedTasks = new ObservableCollection<RelatedTaskViewModel>();
        private ObservableCollection<CommentViewModel> _comments = new ObservableCollection<CommentViewModel>();
        private ObservableCollection<AttachmentViewModel> _attachments = new ObservableCollection<AttachmentViewModel>();

        // Свойство для нового комментария
        private string _newComment = string.Empty;

        #region Свойства
        public string TaskId
        {
            get => _taskId;
            set => this.RaiseAndSetIfChanged(ref _taskId, value);
        }

        public int TaskDbId
        {
            get => _taskDbId;
            set => this.RaiseAndSetIfChanged(ref _taskDbId, value);
        }

        public string TaskName
        {
            get => _taskName;
            set => this.RaiseAndSetIfChanged(ref _taskName, value);
        }

        public string CreatedDate
        {
            get => _createdDate;
            set => this.RaiseAndSetIfChanged(ref _createdDate, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => this.RaiseAndSetIfChanged(ref _statusColor, value);
        }

        public string Priority
        {
            get => _priority;
            set => this.RaiseAndSetIfChanged(ref _priority, value);
        }

        public string PriorityColor
        {
            get => _priorityColor;
            set => this.RaiseAndSetIfChanged(ref _priorityColor, value);
        }

        public string Deadline
        {
            get => _deadline;
            set => this.RaiseAndSetIfChanged(ref _deadline, value);
        }

        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        public string Assignee
        {
            get => _assignee;
            set => this.RaiseAndSetIfChanged(ref _assignee, value);
        }

        public string OrderPosition
        {
            get => _orderPosition;
            set => this.RaiseAndSetIfChanged(ref _orderPosition, value);
        }

        public string StageStatus
        {
            get => _stageStatus;
            set => this.RaiseAndSetIfChanged(ref _stageStatus, value);
        }

        public string Notes
        {
            get => _notes;
            set => this.RaiseAndSetIfChanged(ref _notes, value);
        }

        public string Customer
        {
            get => _customer;
            set => this.RaiseAndSetIfChanged(ref _customer, value);
        }

        public string OrderDeadline
        {
            get => _orderDeadline;
            set => this.RaiseAndSetIfChanged(ref _orderDeadline, value);
        }

        public string OrderStatus
        {
            get => _orderStatus;
            set => this.RaiseAndSetIfChanged(ref _orderStatus, value);
        }

        public ObservableCollection<RelatedTaskViewModel> RelatedTasks
        {
            get => _relatedTasks;
            set => this.RaiseAndSetIfChanged(ref _relatedTasks, value);
        }

        public ObservableCollection<CommentViewModel> Comments
        {
            get => _comments;
            set => this.RaiseAndSetIfChanged(ref _comments, value);
        }

        public ObservableCollection<AttachmentViewModel> Attachments
        {
            get => _attachments;
            set => this.RaiseAndSetIfChanged(ref _attachments, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
        }

        public string NewComment
        {
            get => _newComment;
            set => this.RaiseAndSetIfChanged(ref _newComment, value);
        }
        #endregion

        public ICommand NavigateToTasksCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand DownloadAttachmentCommand { get; }
        public ICommand ViewAttachmentCommand { get; }

        public TaskDetailsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int taskId, Window parentWindow, Guid currentUserId)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _parentWindow = mainWindowViewModel.MainWindow; // Используем главное окно из MainWindowViewModel
            _currentUserId = currentUserId;

            NavigateToTasksCommand = ReactiveCommand.Create(() => _mainWindowViewModel.NavigateToTasks());
            EditTaskCommand = ReactiveCommand.CreateFromTask(EditTaskAsync);
            AddCommentCommand = ReactiveCommand.CreateFromTask(AddCommentAsync);
            DownloadAttachmentCommand = ReactiveCommand.Create<int>(DownloadAttachment);
            ViewAttachmentCommand = ReactiveCommand.Create<int>(ViewAttachment);

            // Загружаем данные асинхронно
            LoadTaskDetailsAsync(taskId);
        }

        private async void LoadTaskDetailsAsync(int taskId)
        {
            IsLoading = true;

            try
            {
                // Пытаемся найти задачу в представлении TaskDetailsView
                var taskView = await _dbContext.TaskDetailsViews
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (taskView != null)
                {
                    // Заполняем данные из представления
                    TaskId = $"T-{taskId}";
                    TaskDbId = taskId;
                    TaskName = taskView.Name ?? "Без названия";
                    Status = taskView.Status ?? "Без статуса";
                    StatusColor = GetStatusColor(taskView.Status);
                    Description = taskView.Description ?? "Нет описания";

                    // Работа с датами
                    if (taskView.EndDate.HasValue)
                    {
                        Deadline = $"Срок: {taskView.EndDate.Value:dd.MM.yyyy}";
                    }
                    else
                    {
                        Deadline = "Срок не указан";
                    }

                    // Приоритет
                    var priorityValue = taskView.Priority ?? 3;
                    Priority = $"Приоритет: {GetPriorityText(priorityValue)}";
                    PriorityColor = GetPriorityColor(priorityValue);

                    // Поля
                    Assignee = taskView.AssigneeName ?? "Не назначен";
                    OrderPosition = $"{taskView.OrderNumber} поз. {taskView.PositionNumber}";
                    StageStatus = taskView.Stage ?? "Не определен";
                    Notes = taskView.Notes ?? "Нет примечаний";
                    Customer = taskView.Customer ?? "Не указан";

                    // Дополнительные настройки
                    CreatedDate = "Создана " + DateTime.Now.AddDays(-5).ToString("dd.MM.yyyy");
                }
                else
                {
                    // Поиск в основной таблице задач с соединениями
                    var task = await _dbContext.Tasks
                        .Include(t => t.Assignee)
                        .Include(t => t.OrderPosition)
                            .ThenInclude(op => op.Order)
                        .FirstOrDefaultAsync(t => t.Id == taskId);

                    if (task != null)
                    {
                        // Заполнение из основной таблицы
                        TaskId = $"T-{task.Id}";
                        TaskDbId = task.Id;
                        TaskName = task.Name;
                        Status = task.Status ?? "Без статуса";
                        StatusColor = GetStatusColor(task.Status);
                        Description = task.Description ?? "Нет описания";

                        // Работа с датами
                        if (task.EndDate.HasValue)
                        {
                            Deadline = $"Срок: {task.EndDate.Value:dd.MM.yyyy}";
                        }
                        else
                        {
                            Deadline = "Срок не указан";
                        }

                        if (task.CreatedAt.HasValue)
                        {
                            CreatedDate = $"Создана {task.CreatedAt.Value:dd.MM.yyyy}";
                        }
                        else
                        {
                            CreatedDate = "Дата создания не указана";
                        }

                        // Приоритет
                        Priority = $"Приоритет: {GetPriorityText(task.Priority)}";
                        PriorityColor = GetPriorityColor(task.Priority);

                        // Поля с проверками на null
                        Assignee = task.Assignee?.FirstName + " " + task.Assignee?.LastName ?? "Не назначен";

                        if (task.OrderPosition != null)
                        {
                            OrderPosition = $"{task.OrderPosition.Order?.OrderNumber} поз. {task.OrderPosition.PositionNumber}";
                            Customer = task.OrderPosition.Order?.Customer ?? "Не указан";

                            if (task.OrderPosition.Order?.ContractDeadline != null)
                            {
                                OrderDeadline = task.OrderPosition.Order.ContractDeadline.Value.ToString("dd.MM.yyyy");
                            }
                            else
                            {
                                OrderDeadline = "Не указан";
                            }

                            OrderStatus = task.OrderPosition.Order?.Status ?? "Неизвестно";
                        }
                        else
                        {
                            OrderPosition = "Не указана";
                            Customer = "Не указан";
                            OrderDeadline = "Не указан";
                            OrderStatus = "Неизвестно";
                        }

                        StageStatus = task.Stage ?? "Не определен";
                        Notes = task.Notes ?? "Нет примечаний";
                    }
                    else
                    {
                        // Если задача не найдена, заполняем заглушками
                        TaskId = $"T-{taskId}";
                        TaskDbId = taskId;
                        TaskName = "Задача не найдена";
                        Status = "Ошибка";
                        StatusColor = "#FF7043";
                        Description = "Не удалось найти данные задачи с указанным ID.";
                    }
                }

                // Загрузим связанные задачи, комментарии и вложения
                await LoadRelatedTasksAsync(taskId);
                await LoadCommentsAsync(taskId);
                await LoadAttachmentsAsync(taskId);
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                TaskName = "Ошибка загрузки";
                Description = $"Произошла ошибка при загрузке данных: {ex.Message}";
                Status = "Ошибка";
                StatusColor = "#FF7043";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task LoadRelatedTasksAsync(int taskId)
        {
            try
            {
                // Находим задачу, чтобы получить OrderPositionId
                var task = await _dbContext.Tasks
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task?.OrderPositionId != null)
                {
                    // Находим все задачи с тем же OrderPositionId, кроме текущей
                    var relatedTasks = await _dbContext.Tasks
                        .Where(t => t.OrderPositionId == task.OrderPositionId && t.Id != taskId)
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(5)
                        .ToListAsync();

                    // Преобразуем в модель представления
                    var relatedTasksVM = relatedTasks.Select(t => new RelatedTaskViewModel
                    {
                        Id = t.Id,
                        Name = $"T-{t.Id}: {t.Name}",
                        Status = t.Status ?? "Нет статуса",
                        StatusColor = GetStatusColor(t.Status)
                    }).ToList();

                    // Обновляем коллекцию
                    RelatedTasks = new ObservableCollection<RelatedTaskViewModel>(relatedTasksVM);
                }
            }
            catch (Exception)
            {
                // В случае ошибки просто оставляем пустую коллекцию
            }
        }

        private async System.Threading.Tasks.Task LoadCommentsAsync(int taskId)
        {
            try
            {
                // Загружаем комментарии для задачи
                var comments = await _dbContext.Comments
                    .Include(c => c.User)
                    .Where(c => c.TaskId == taskId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                // Преобразуем в модель представления
                var commentVMs = comments.Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Text = c.Text,
                    Author = c.User != null ? $"{c.User.FirstName} {c.User.LastName}" : "Неизвестный пользователь",
                    CreatedDate = c.CreatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Неизвестная дата",
                    UserInitials = GetUserInitials(c.User)
                }).ToList();

                // Обновляем коллекцию
                Comments = new ObservableCollection<CommentViewModel>(commentVMs);
            }
            catch (Exception)
            {
                // В случае ошибки просто оставляем пустую коллекцию
            }
        }

        private async System.Threading.Tasks.Task LoadAttachmentsAsync(int taskId)
        {
            try
            {
                // Загружаем вложения для задачи
                var attachments = await _dbContext.Attachments
                    .Include(a => a.UploadedByNavigation)
                    .Where(a => a.TaskId == taskId)
                    .OrderByDescending(a => a.UploadedAt)
                    .ToListAsync();

                // Преобразуем в модель представления
                var attachmentVMs = attachments.Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileType = a.FileType ?? "Неизвестный тип",
                    FileSize = FormatFileSize(a.FileSize),
                    UploadedBy = a.UploadedByNavigation != null ? $"{a.UploadedByNavigation.FirstName} {a.UploadedByNavigation.LastName}" : "Неизвестный пользователь",
                    UploadedDate = a.UploadedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Неизвестная дата"
                }).ToList();

                // Обновляем коллекцию
                Attachments = new ObservableCollection<AttachmentViewModel>(attachmentVMs);
            }
            catch (Exception)
            {
                // В случае ошибки просто оставляем пустую коллекцию
            }
        }

        private string GetStatusColor(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return "#666666";

            return status switch
            {
                "Готово" or "Завершено" => "#4CAF9D",
                "В процессе" or "В работе" => "#00ACC1",
                "В очереди" => "#9575CD",
                "Ожидание" => "#FFB74D",
                "Ждем производство" => "#FF9800",
                "Отменено" => "#F44336",
                _ => "#666666"
            };
        }

        private string GetPriorityText(int priority)
        {
            return priority switch
            {
                1 => "Высокий",
                2 => "Средний",
                3 => "Низкий",
                _ => "Стандартный"
            };
        }

        private string GetPriorityColor(int priority)
        {
            return priority switch
            {
                1 => "#FF7043", // Высокий - красный
                2 => "#FFB74D", // Средний - оранжевый
                3 => "#9575CD", // Низкий - синий 
                _ => "#666666"  // По умолчанию - серый
            };
        }

        private string GetUserInitials(User user)
        {
            if (user == null)
                return "??";

            string firstName = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName.Substring(0, 1) : "?";
            string lastName = !string.IsNullOrEmpty(user.LastName) ? user.LastName.Substring(0, 1) : "?";

            return $"{firstName}{lastName}";
        }

        private string FormatFileSize(long? bytes)
        {
            if (bytes == null || bytes <= 0)
                return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = (long)bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        // В TaskDetailsViewModel.cs нужно исправить метод EditTaskAsync
        private async System.Threading.Tasks.Task EditTaskAsync()
        {
            System.Diagnostics.Debug.WriteLine("Начало метода EditTaskAsync");
            try
            {
                IsLoading = true;

                // Если _parentWindow все еще null, используйте глобальное окно
                var parentWindow = _parentWindow ?? AppWindows.MainWindow;
                System.Diagnostics.Debug.WriteLine($"Используемое окно: {parentWindow != null}");

                System.Diagnostics.Debug.WriteLine($"TaskDbId: {TaskDbId}");

                // Получаем задачу из БД для редактирования
                var task = await _dbContext.Tasks.FindAsync(TaskDbId);
                System.Diagnostics.Debug.WriteLine($"Задача найдена: {task != null}");

                if (task == null)
                {
                    System.Diagnostics.Debug.WriteLine("Задача не найдена в БД");
                    return;
                }

                // Используем TaskEditDialog для редактирования задачи
                System.Diagnostics.Debug.WriteLine("Вызываем TaskEditDialog.ShowDialog");
                var updatedTask = await TaskEditDialog.ShowDialog(parentWindow, _dbContext, _currentUserId, task);
                System.Diagnostics.Debug.WriteLine($"Результат диалога: {updatedTask != null}");

                if (updatedTask != null)
                {
                    // Обновляем данные на странице
                    System.Diagnostics.Debug.WriteLine("Обновляем данные на странице");
                    LoadTaskDetailsAsync(TaskDbId);
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                System.Diagnostics.Debug.WriteLine($"Ошибка при редактировании задачи: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("Конец метода EditTaskAsync");
            }
        }

        private async System.Threading.Tasks.Task AddCommentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewComment))
                return;

            try
            {
                // Создаем новый комментарий
                var comment = new Comment
                {
                    TaskId = TaskDbId,
                    UserId = _currentUserId,
                    Text = NewComment,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Добавляем в БД
                _dbContext.Comments.Add(comment);
                await _dbContext.SaveChangesAsync();

                // Очищаем поле ввода
                NewComment = string.Empty;

                // Обновляем список комментариев
                await LoadCommentsAsync(TaskDbId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении комментария: {ex.Message}");
            }
        }

        private void DownloadAttachment(int attachmentId)
        {
            // Логика для скачивания вложения
            // TODO: Реализовать в будущем
            System.Diagnostics.Debug.WriteLine($"Скачивание вложения с ID: {attachmentId}");
        }

        private void ViewAttachment(int attachmentId)
        {
            // Логика для просмотра вложения
            // TODO: Реализовать в будущем
            System.Diagnostics.Debug.WriteLine($"Просмотр вложения с ID: {attachmentId}");
        }
    }

    public class RelatedTaskViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#666666";
    }

    public class CommentViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string UserInitials { get; set; } = string.Empty;
    }

    public class AttachmentViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public string UploadedDate { get; set; } = string.Empty;
    }
}