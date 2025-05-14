using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;

namespace ManufactPlanner.Services
{
    public class NotificationService : IDisposable
    {
        private PostgresContext _dbContext;
        private NpgsqlConnection _listenConnection;
        private CancellationTokenSource _cts;
        private System.Threading.Tasks.Task _monitoringTask;
        private System.Threading.Tasks.Task _deadlineCheckTask;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<NotificationViewModel> _newNotifications = new Subject<NotificationViewModel>();
        private readonly string _connectionString;
        private MainWindowViewModel _mainViewModel;
        private bool _isInitialized = false;

        // Используем Singleton паттерн для сервиса уведомлений
        private static NotificationService _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        // Свойство для доступа к потоку новых уведомлений
        public IObservable<NotificationViewModel> NewNotifications => _newNotifications.AsObservable();

        // Флаг, показывающий, работает ли сервис в данный момент
        public bool IsRunning { get; private set; }


        private readonly HashSet<string> _processedNotificationIds = new HashSet<string>();

        private System.Threading.Timer _cleanupTimer;
        private NotificationService()
        {
            // Инициализируем строку подключения из PostgresContext
            _connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123";

            _cleanupTimer = new System.Threading.Timer(CleanupProcessedNotifications, null,
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }
        // Метод очистки старых обработанных уведомлений
        private void CleanupProcessedNotifications(object state)
        {
            try
            {
                // Если коллекция слишком большая, очищаем её
                if (_processedNotificationIds.Count > 1000)
                {
                    lock (_processedNotificationIds)
                    {
                        // Оставляем только последние 100 уведомлений
                        _processedNotificationIds.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке обработанных уведомлений: {ex.Message}");
            }
        }

        public async System.Threading.Tasks.Task Initialize(PostgresContext dbContext, MainWindowViewModel mainViewModel)
        {
            if (_isInitialized)
                return;

            _dbContext = dbContext;
            _mainViewModel = mainViewModel;
            _isInitialized = true;

            try
            {
                // Создаем триггер в базе данных, если его еще нет
                await CreateNotificationTriggersAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при создании триггера: {ex.Message}");
            }
        }

        public void Start()
        {
            if (IsRunning || !_isInitialized)
                return;

            _cts = new CancellationTokenSource();

            // Запускаем задачу мониторинга базы данных
            _monitoringTask = System.Threading.Tasks.Task.Run(() => MonitorDatabaseChangesAsync(_cts.Token));

            // Запускаем задачу проверки дедлайнов
            _deadlineCheckTask = System.Threading.Tasks.Task.Run(() => CheckDeadlinesAsync(_cts.Token));

            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            // Отменяем задачи мониторинга
            _cts?.Cancel();

            try
            {
                // Ждем завершения задач
                System.Threading.Tasks.Task.WaitAll(new[] { _monitoringTask, _deadlineCheckTask }, 2000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при остановке сервиса уведомлений: {ex.Message}");
            }

            _listenConnection?.Close();
            _listenConnection = null;

            IsRunning = false;
        }

        // Метод для создания триггеров и функций в базе данных
        private async System.Threading.Tasks.Task CreateNotificationTriggersAsync()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Создаем функцию для отправки уведомлений
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = connection;

                    // Функция для отправки уведомлений
                    cmd.CommandText = @"
                    CREATE OR REPLACE FUNCTION notify_task_changes()
                    RETURNS trigger AS $$
                    DECLARE
                        payload TEXT;
                        task_user_id UUID;
                        task_user_record RECORD;
                    BEGIN
                        IF (TG_OP = 'INSERT') THEN
                            payload := json_build_object(
                                'operation', TG_OP,
                                'task_id', NEW.id,
                                'task_name', NEW.name,
                                'assignee_id', NEW.assignee_id,
                                'status', NEW.status,
                                'timestamp', EXTRACT(EPOCH FROM NOW())
                            )::text;
        
                            -- Вставляем уведомление в таблицу notifications
                            IF NEW.assignee_id IS NOT NULL THEN
                                INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                VALUES(
                                    NEW.assignee_id, 
                                    'Новая задача', 
                                    'Вам назначена задача: ' || NEW.name, 
                                    false, 
                                    NOW(),
                                    '/tasks/' || NEW.id,
                                    'task_assigned'
                                );
                            END IF;
        
                            -- Уведомление для создателя задачи, если это не исполнитель
                            IF NEW.created_by IS NOT NULL AND NEW.created_by <> NEW.assignee_id THEN
                                INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                VALUES(
                                    NEW.created_by, 
                                    'Создана новая задача', 
                                    'Создана задача: ' || NEW.name, 
                                    false, 
                                    NOW(),
                                    '/tasks/' || NEW.id,
                                    'task_created'
                                );
                            END IF;
        
                            -- Уведомления для соисполнителей из связанной таблицы
                            FOR task_user_record IN SELECT user_id FROM task_users WHERE task_id = NEW.id AND role = 'co_assignee' LOOP
                                INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                VALUES(
                                    task_user_record.user_id, 
                                    'Назначена задача как соисполнителю', 
                                    'Вы назначены соисполнителем задачи: ' || NEW.name, 
                                    false, 
                                    NOW(),
                                    '/tasks/' || NEW.id,
                                    'task_assigned'
                                );
                            END LOOP;
        
                        ELSIF (TG_OP = 'UPDATE') THEN
                            -- Если изменился статус задачи
                            IF OLD.status <> NEW.status THEN
                                payload := json_build_object(
                                    'operation', 'STATUS_CHANGE',
                                    'task_id', NEW.id,
                                    'task_name', NEW.name,
                                    'assignee_id', NEW.assignee_id,
                                    'old_status', OLD.status,
                                    'new_status', NEW.status,
                                    'timestamp', EXTRACT(EPOCH FROM NOW())
                                )::text;
            
                                -- Уведомление для исполнителя
                                IF NEW.assignee_id IS NOT NULL THEN
                                    INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                    VALUES(
                                        NEW.assignee_id, 
                                        'Изменение статуса задачи', 
                                        'Задача ' || NEW.name || ' изменила статус с ' || OLD.status || ' на ' || NEW.status, 
                                        false, 
                                        NOW(),
                                        '/tasks/' || NEW.id,
                                        'status_changed'
                                    );
                                END IF;
            
                                -- Уведомление для создателя задачи, если это не исполнитель
                                IF NEW.created_by IS NOT NULL AND NEW.created_by <> NEW.assignee_id THEN
                                    INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                    VALUES(
                                        NEW.created_by, 
                                        'Изменение статуса задачи', 
                                        'Задача ' || NEW.name || ' изменила статус с ' || OLD.status || ' на ' || NEW.status, 
                                        false, 
                                        NOW(),
                                        '/tasks/' || NEW.id,
                                        'status_changed'
                                    );
                                END IF;
            
                                -- Уведомления для соисполнителей
                                FOR task_user_record IN SELECT user_id FROM task_users WHERE task_id = NEW.id AND role = 'co_assignee' LOOP
                                    INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                    VALUES(
                                        task_user_record.user_id, 
                                        'Изменение статуса задачи', 
                                        'Задача ' || NEW.name || ' изменила статус с ' || OLD.status || ' на ' || NEW.status, 
                                        false, 
                                        NOW(),
                                        '/tasks/' || NEW.id,
                                        'status_changed'
                                    );
                                END LOOP;
                            END IF;
        
                            -- Если изменился исполнитель задачи
                            IF OLD.assignee_id IS DISTINCT FROM NEW.assignee_id AND NEW.assignee_id IS NOT NULL THEN
                                payload := json_build_object(
                                    'operation', 'ASSIGNEE_CHANGE',
                                    'task_id', NEW.id,
                                    'task_name', NEW.name,
                                    'old_assignee_id', OLD.assignee_id,
                                    'new_assignee_id', NEW.assignee_id,
                                    'timestamp', EXTRACT(EPOCH FROM NOW())
                                )::text;
            
                                -- Уведомление для нового исполнителя
                                INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                VALUES(
                                    NEW.assignee_id, 
                                    'Назначена задача', 
                                    'Вам назначена задача: ' || NEW.name, 
                                    false, 
                                    NOW(),
                                    '/tasks/' || NEW.id,
                                    'task_assigned'
                                );
            
                                -- Уведомление для старого исполнителя, если он был
                                IF OLD.assignee_id IS NOT NULL THEN
                                    INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                    VALUES(
                                        OLD.assignee_id, 
                                        'Переназначение задачи', 
                                        'Задача ' || NEW.name || ' переназначена другому исполнителю', 
                                        false, 
                                        NOW(),
                                        '/tasks/' || NEW.id,
                                        'task_reassigned'
                                    );
                                END IF;
                            END IF;
                        END IF;
    
                        -- Отправляем уведомление через механизм NOTIFY
                        IF payload IS NOT NULL THEN
                            PERFORM pg_notify('task_changes', payload);
                        END IF;
    
                        RETURN NEW;
                    END;
                    $$ LANGUAGE plpgsql;";

                    await cmd.ExecuteNonQueryAsync();

                    // Функция для обработки комментариев
                    cmd.CommandText = @"
                    CREATE OR REPLACE FUNCTION notify_comment_changes()
                    RETURNS trigger AS $$
                    DECLARE
                        task_assignee_id UUID;
                        task_creator_id UUID;
                        task_name TEXT;
                        payload TEXT;
                        task_user_record RECORD;
                    BEGIN
                        IF (TG_OP = 'INSERT') THEN
                            -- Получаем информацию о задаче, её исполнителе и создателе
                            SELECT t.assignee_id, t.name, t.created_by INTO task_assignee_id, task_name, task_creator_id
                            FROM tasks t
                            WHERE t.id = NEW.task_id;
        
                            payload := json_build_object(
                                'operation', 'NEW_COMMENT',
                                'comment_id', NEW.id,
                                'task_id', NEW.task_id,
                                'user_id', NEW.user_id,
                                'timestamp', EXTRACT(EPOCH FROM NOW())
                            )::text;
        
                            -- Уведомление для исполнителя задачи, если это не автор комментария
                            IF task_assignee_id IS NOT NULL AND task_assignee_id <> NEW.user_id THEN
                                INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                VALUES(
                                    task_assignee_id, 
                                    'Новый комментарий', 
                                    'Новый комментарий в задаче: ' || task_name, 
                                    false, 
                                    NOW(),
                                    '/tasks/' || NEW.task_id,
                                    'new_comment'
                                );
                            END IF;
        
                            -- Уведомление для создателя задачи, если это не автор комментария и не исполнитель
                            IF task_creator_id IS NOT NULL AND task_creator_id <> NEW.user_id AND task_creator_id <> task_assignee_id THEN
                                INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                VALUES(
                                    task_creator_id, 
                                    'Новый комментарий', 
                                    'Новый комментарий в задаче: ' || task_name, 
                                    false, 
                                    NOW(),
                                    '/tasks/' || NEW.task_id,
                                    'new_comment'
                                );
                            END IF;
        
                            -- Уведомления для соисполнителей, если они не являются автором комментария
                            FOR task_user_record IN SELECT user_id FROM task_users WHERE task_id = NEW.task_id AND role = 'co_assignee' AND user_id <> NEW.user_id LOOP
                                INSERT INTO notifications(user_id, title, message, is_read, created_at, link_to, notification_type)
                                VALUES(
                                    task_user_record.user_id, 
                                    'Новый комментарий', 
                                    'Новый комментарий в задаче: ' || task_name, 
                                    false, 
                                    NOW(),
                                    '/tasks/' || NEW.task_id,
                                    'new_comment'
                                );
                            END LOOP;
                        END IF;
    
                        -- Отправляем уведомление через механизм NOTIFY
                        IF payload IS NOT NULL THEN
                            PERFORM pg_notify('comment_changes', payload);
                        END IF;
    
                        RETURN NEW;
                    END;
                    $$ LANGUAGE plpgsql;";

                    await cmd.ExecuteNonQueryAsync();

                    // Триггер для задач
                    cmd.CommandText = @"
                    DROP TRIGGER IF EXISTS task_notification_trigger ON tasks;
                    CREATE TRIGGER task_notification_trigger
                    AFTER INSERT OR UPDATE ON tasks
                    FOR EACH ROW
                    EXECUTE FUNCTION notify_task_changes();";

                    await cmd.ExecuteNonQueryAsync();

                    // Триггер для комментариев
                    cmd.CommandText = @"
                    DROP TRIGGER IF EXISTS comment_notification_trigger ON comments;
                    CREATE TRIGGER comment_notification_trigger
                    AFTER INSERT ON comments
                    FOR EACH ROW
                    EXECUTE FUNCTION notify_comment_changes();";

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // Метод для мониторинга изменений в базе данных
        private async System.Threading.Tasks.Task MonitorDatabaseChangesAsync(CancellationToken token)
        {
            try
            {
                // Создаем новое соединение для LISTEN/NOTIFY
                _listenConnection = new NpgsqlConnection(_connectionString);
                await _listenConnection.OpenAsync(token);

                // Подписываемся на уведомления о задачах и комментариях
                using (var cmd = new NpgsqlCommand("LISTEN task_changes; LISTEN comment_changes;", _listenConnection))
                {
                    await cmd.ExecuteNonQueryAsync(token);
                }

                // Обработчик уведомлений
                _listenConnection.Notification += OnDatabaseNotification;

                // Бесконечный цикл для поддержания соединения
                while (!token.IsCancellationRequested)
                {
                    await _listenConnection.WaitAsync(token);
                }
            }
            catch (OperationCanceledException)
            {
                // Ожидаемое исключение при отмене
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при мониторинге базы данных: {ex.Message}");

                // Попытка переподключения через 5 секунд
                if (!token.IsCancellationRequested)
                {
                    await System.Threading.Tasks.Task.Delay(5000, token);
                    _ = System.Threading.Tasks.Task.Run(() => MonitorDatabaseChangesAsync(token));
                }
            }
            finally
            {
                if (_listenConnection != null)
                {
                    _listenConnection.Notification -= OnDatabaseNotification;
                    _listenConnection.Close();
                }
            }
        }

        // В методе OnDatabaseNotification в классе NotificationService добавим проверку и использование наших сервисов:

        private void OnDatabaseNotification(object sender, NpgsqlNotificationEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Payload))
                    return;

                var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Payload);

                if (payload == null)
                    return;

                // Проверка на пустое уведомление
                if (payload.Count < 2 || !payload.ContainsKey("operation") || !payload.ContainsKey("task_id"))
                    return;

                string operation = payload["operation"].ToString();
                int taskId = Convert.ToInt32(payload["task_id"]);
                string notificationType = string.Empty;
                string title = string.Empty;
                string message = string.Empty;
                Guid? targetUserId = null;

                // Получаем ID пользователя, которому предназначено уведомление
                if (payload.ContainsKey("assignee_id") && payload["assignee_id"] != null)
                {
                    targetUserId = Guid.Parse(payload["assignee_id"].ToString());
                }

                // Формируем данные для уведомления в зависимости от операции
                switch (operation)
                {
                    case "INSERT":
                        title = "Новая задача";
                        message = $"Создана новая задача: {payload["task_name"]}";
                        notificationType = "task_created";
                        break;

                    case "STATUS_CHANGE":
                        title = "Изменение статуса";
                        message = $"Задача {payload["task_name"]} изменила статус с {payload["old_status"]} на {payload["new_status"]}";
                        notificationType = "status_changed";
                        break;

                    case "ASSIGNEE_CHANGE":
                        title = "Назначен исполнитель";
                        message = $"К задаче {payload["task_name"]} назначен новый исполнитель";
                        notificationType = "assignee_changed";
                        targetUserId = payload.ContainsKey("new_assignee_id") ?
                            Guid.Parse(payload["new_assignee_id"].ToString()) : null;
                        break;

                    case "NEW_COMMENT":
                        title = "Новый комментарий";
                        message = $"Добавлен новый комментарий к задаче";
                        notificationType = "new_comment";
                        break;

                    default:
                        // Неизвестная операция - пропускаем
                        return;
                }

                // Создаем уникальный идентификатор для уведомления
                string notificationId = $"{operation}_{taskId}_{notificationType}_{DateTime.Now.Ticks}";

                // Проверяем, было ли это уведомление уже обработано
                if (_processedNotificationIds.Contains(notificationId))
                {
                    return; // Уведомление уже было обработано, выходим
                }

                // Добавляем уникальный идентификатор в коллекцию обработанных уведомлений
                _processedNotificationIds.Add(notificationId);

                // Создаем объект уведомления
                var notification = new NotificationViewModel
                {
                    Title = title,
                    Message = message,
                    Type = notificationType,
                    Timestamp = DateTime.Now,
                    TaskId = taskId
                };

                // Отправляем уведомление через поток
                _newNotifications.OnNext(notification);

                // Показываем десктопное уведомление только если текущий пользователь является целевым
                if (ShouldShowDesktopNotification() &&
                    _mainViewModel != null &&
                    _mainViewModel.CurrentUserId != Guid.Empty &&
                    (targetUserId == null || targetUserId == _mainViewModel.CurrentUserId))
                {
                    ShowDesktopNotification(notification);
                }

                // Обновляем счетчик непрочитанных уведомлений в UI только для текущего пользователя
                if (_mainViewModel != null && _mainViewModel.CurrentUserId != Guid.Empty)
                {
                    UpdateUnreadNotificationsCount();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обработке уведомления: {ex.Message}");
            }
        }

        // Добавляем новые методы для проверки настроек и отправки email
        private bool ShouldShowDesktopNotification()
        {
            try
            {
                if (_mainViewModel == null || _mainViewModel.CurrentUserId == Guid.Empty)
                    return false; // По умолчанию не показываем, если нет текущего пользователя

                // Проверяем настройку в MainViewModel (которая кэширует настройку из БД)
                return _mainViewModel.NotifyDesktopEnabled;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке настройки уведомлений: {ex.Message}");
                return false; // В случае ошибки не показываем уведомления
            }
        }

        private async void SendEmailNotificationAsync(NotificationViewModel notification)
        {
            if (_mainViewModel == null || _mainViewModel.CurrentUserId == Guid.Empty)
                return;

            try
            {
                // Проверяем настройку email уведомлений у текущего пользователя
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == _mainViewModel.CurrentUserId);

                if (user == null || string.IsNullOrEmpty(user.Email) || !EmailService.Instance.IsValidEmail(user.Email))
                    return;

                var settings = await _dbContext.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == _mainViewModel.CurrentUserId);

                // Проверяем, что пользователь включил email-уведомления
                if (settings?.NotifyEmail != true)
                    return;

                // Проверяем, что включен соответствующий тип уведомления
                bool shouldSend = notification.Type switch
                {
                    "task_assigned" => settings.NotifyNewTasks ?? true,
                    "status_changed" => settings.NotifyStatusChanges ?? true,
                    "new_comment" => settings.NotifyComments ?? true,
                    "deadline_approaching" => settings.NotifyDeadlines ?? true,
                    _ => true // Для других типов уведомлений отправляем по умолчанию
                };

                if (!shouldSend)
                    return;

                // Отправляем email
                await EmailService.Instance.SendNotificationEmailAsync(
                    user.Email,
                    notification.Title,
                    notification.Message
                );

                Debug.WriteLine($"Email уведомление отправлено на адрес {user.Email}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при отправке email: {ex.Message}");
            }
        }

        // Метод для отображения уведомления на рабочем столе
        private void ShowDesktopNotification(NotificationViewModel notification)
        {
            try
            {
                // Показываем уведомление через трэй если окно скрыто
                if (_mainViewModel?.MainWindow?.IsVisible == false)
                {
                    _mainViewModel?.TrayService?.ShowTrayNotification(
                        notification.Title,
                        notification.Message);
                }

                // Показываем диалог уведомления если окно видимо
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    NotificationDialogService.ShowNotificationDialog(notification, _mainViewModel, this);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при показе десктопного уведомления: {ex.Message}");
            }
        }

        // Метод для проверки приближающихся дедлайнов
        private async System.Threading.Tasks.Task CheckDeadlinesAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Проверяем каждые 30 минут
                    await System.Threading.Tasks.Task.Delay(30 * 60 * 1000, token);

                    // Текущая дата
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var tomorrow = today.AddDays(1);

                    // Находим задачи с дедлайном на сегодня или завтра, которые еще не выполнены
                    var tasksWithDeadlines = await _dbContext.Tasks
                        .Where(t => (t.EndDate == today || t.EndDate == tomorrow) &&
                                   t.Status != "Готово" && t.Status != "Завершено")
                        .ToListAsync(token);

                    foreach (var task in tasksWithDeadlines)
                    {
                        if (task.AssigneeId == null)
                            continue;

                        // Проверяем, было ли уже отправлено уведомление об этом дедлайне
                        var existingNotification = await _dbContext.Notifications
                            .Where(n => n.UserId == task.AssigneeId &&
                                      n.NotificationType == "deadline_approaching" &&
                                      n.LinkTo == $"/tasks/{task.Id}" &&
                                      n.CreatedAt >= DateTime.Today)
                            .AnyAsync(token);

                        if (!existingNotification)
                        {
                            // Создаем уведомление о приближающемся дедлайне для основного исполнителя
                            var notification = new Notification
                            {
                                UserId = task.AssigneeId,
                                Title = "Приближается срок выполнения",
                                Message = $"Задача '{task.Name}' должна быть выполнена {(task.EndDate == today ? "сегодня" : "завтра")}",
                                IsRead = false,
                                CreatedAt = DateTime.Now,
                                LinkTo = $"/tasks/{task.Id}",
                                NotificationType = "deadline_approaching"
                            };

                            _dbContext.Notifications.Add(notification);

                            // Создаем уведомление для UI
                            var notificationViewModel = new NotificationViewModel
                            {
                                Title = notification.Title,
                                Message = notification.Message,
                                Type = notification.NotificationType,
                                Timestamp = notification.CreatedAt ?? DateTime.Now,
                                TaskId = task.Id
                            };

                            // Отправляем уведомление через поток
                            _newNotifications.OnNext(notificationViewModel);

                            // Показываем десктопное уведомление для текущего пользователя
                            if (task.AssigneeId == _mainViewModel?.CurrentUserId)
                            {
                                ShowDesktopNotification(notificationViewModel);
                            }
                        }

                        // Получаем всех соисполнителей задачи
                        var coAssignees = await _dbContext.TaskUsers
                            .Where(tu => tu.TaskId == task.Id && tu.Role == "co_assignee")
                            .ToListAsync(token);

                        foreach (var coAssignee in coAssignees)
                        {
                            // Проверяем, было ли уже отправлено уведомление этому соисполнителю
                            existingNotification = await _dbContext.Notifications
                                .Where(n => n.UserId == coAssignee.UserId &&
                                          n.NotificationType == "deadline_approaching" &&
                                          n.LinkTo == $"/tasks/{task.Id}" &&
                                          n.CreatedAt >= DateTime.Today)
                                .AnyAsync(token);

                            if (!existingNotification)
                            {
                                // Создаем уведомление для соисполнителя
                                var notification = new Notification
                                {
                                    UserId = coAssignee.UserId,
                                    Title = "Приближается срок выполнения",
                                    Message = $"Задача '{task.Name}', где вы соисполнитель, должна быть выполнена {(task.EndDate == today ? "сегодня" : "завтра")}",
                                    IsRead = false,
                                    CreatedAt = DateTime.Now,
                                    LinkTo = $"/tasks/{task.Id}",
                                    NotificationType = "deadline_approaching"
                                };

                                _dbContext.Notifications.Add(notification);

                                // Показываем десктопное уведомление для текущего пользователя
                                if (coAssignee.UserId == _mainViewModel?.CurrentUserId)
                                {
                                    var notificationViewModel = new NotificationViewModel
                                    {
                                        Title = notification.Title,
                                        Message = notification.Message,
                                        Type = notification.NotificationType,
                                        Timestamp = notification.CreatedAt ?? DateTime.Now,
                                        TaskId = task.Id
                                    };

                                    _newNotifications.OnNext(notificationViewModel);
                                    ShowDesktopNotification(notificationViewModel);
                                }
                            }
                        }
                    }

                    await _dbContext.SaveChangesAsync(token);

                    // Обновляем счетчик непрочитанных уведомлений в UI только для текущего пользователя
                    if (_mainViewModel?.CurrentUserId != Guid.Empty)
                    {
                        await UpdateUnreadCountAsync(_mainViewModel.CurrentUserId);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ожидаемое исключение при отмене
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при проверке дедлайнов: {ex.Message}");

                    // Ждем 5 минут перед повторной попыткой
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(5 * 60 * 1000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        // Метод для обновления счетчика непрочитанных уведомлений в UI
        private async void UpdateUnreadNotificationsCount()
        {
            try
            {
                if (_mainViewModel == null || _mainViewModel.CurrentUserId == Guid.Empty)
                    return;

                var unreadCount = await _dbContext.Notifications
                    .CountAsync(n => n.UserId == _mainViewModel.CurrentUserId && n.IsRead != true);

                // Обновляем счетчик в UI через диспетчер потоков Avalonia
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _mainViewModel.UnreadNotificationsCount = unreadCount;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении счетчика уведомлений: {ex.Message}");
            }
        }
        // Добавьте этот метод в класс NotificationService
        public async System.Threading.Tasks.Task UpdateUnreadCountAsync(Guid userId)
        {
            try
            {
                if (_dbContext == null || userId == Guid.Empty)
                    return;

                var unreadCount = await _dbContext.Notifications
                    .CountAsync(n => n.UserId == userId && n.IsRead != true);

                // Обновляем счетчик в UI через диспетчер потоков Avalonia
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_mainViewModel != null)
                    {
                        _mainViewModel.UnreadNotificationsCount = unreadCount;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении счетчика уведомлений: {ex.Message}");
            }
        }
        // Метод для получения непрочитанных уведомлений для текущего пользователя
        public async Task<List<NotificationViewModel>> GetUnreadNotificationsAsync(Guid userId)
        {
            try
            {
                if (_dbContext == null)
                    return new List<NotificationViewModel>();

                var notifications = await _dbContext.Notifications
                    .Where(n => n.UserId == userId && n.IsRead != true)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(20) // Ограничиваем количество уведомлений
                    .ToListAsync();

                return notifications.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.NotificationType,
                    Timestamp = n.CreatedAt ?? DateTime.Now,
                    TaskId = ExtractTaskIdFromLink(n.LinkTo)
                }).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении уведомлений: {ex.Message}");
                return new List<NotificationViewModel>();
            }
        }

        // Метод для маркировки уведомления как прочитанного
        public async System.Threading.Tasks.Task MarkNotificationAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _dbContext.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    await _dbContext.SaveChangesAsync();

                    // Обновляем счетчик непрочитанных уведомлений
                    UpdateUnreadNotificationsCount();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при маркировке уведомления как прочитанного: {ex.Message}");
            }
        }

        // Метод для маркировки всех уведомлений как прочитанных
        public async System.Threading.Tasks.Task MarkAllNotificationsAsReadAsync(Guid userId)
        {
            try
            {
                var notifications = await _dbContext.Notifications
                    .Where(n => n.UserId == userId && n.IsRead != true)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _dbContext.SaveChangesAsync();

                // Обновляем счетчик непрочитанных уведомлений
                UpdateUnreadNotificationsCount();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при маркировке всех уведомлений как прочитанных: {ex.Message}");
            }
        }

        // Вспомогательный метод для извлечения ID задачи из ссылки
        private int ExtractTaskIdFromLink(string link)
        {
            if (string.IsNullOrEmpty(link))
                return 0;

            // Ожидаемый формат: "/tasks/{id}"
            try
            {
                var parts = link.Split('/');
                if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int id))
                {
                    return id;
                }
            }
            catch { }

            return 0;
        }

        // Реализация интерфейса IDisposable
        public void Dispose()
        {
            Stop();
            _cleanupTimer?.Dispose();
            _disposables.Dispose();
        }
        // В NotificationService добавьте:
        public async Task<List<NotificationViewModel>> GetAllNotificationsAsync(Guid userId)
        {
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100) // Ограничение для производительности
                .ToListAsync();

            return notifications.Select(n => new NotificationViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt ?? DateTime.Now,
                IsRead = n.IsRead ?? false,
                TaskId = ParseTaskId(n.LinkTo),
                LinkTo = n.LinkTo
            }).ToList();
        }

        private int ParseTaskId(string linkTo)
        {
            if (string.IsNullOrEmpty(linkTo))
                return 0;

            if (linkTo.StartsWith("/tasks/") && int.TryParse(linkTo.Substring(7), out int taskId))
                return taskId;

            return 0;
        }
    }

    // Модель представления для уведомления
    public class NotificationViewModel : ViewModelBase
    {
        private int _id;
        private string _title;
        private string _message;
        private DateTime _createdAt;
        private bool _isRead;
        private int _taskId;
        private string _linkTo;

        public string Type { get; set; }
        public DateTime Timestamp { get; set; }

        public int Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => this.RaiseAndSetIfChanged(ref _createdAt, value);
        }

        public bool IsRead
        {
            get => _isRead;
            set => this.RaiseAndSetIfChanged(ref _isRead, value);
        }

        public int TaskId
        {
            get => _taskId;
            set => this.RaiseAndSetIfChanged(ref _taskId, value);
        }

        public string LinkTo
        {
            get => _linkTo;
            set => this.RaiseAndSetIfChanged(ref _linkTo, value);
        }

        public string FormattedTime => CreatedAt.ToString("HH:mm");
        public string FormattedDate => CreatedAt.ToString("dd.MM.yyyy");

    }
}