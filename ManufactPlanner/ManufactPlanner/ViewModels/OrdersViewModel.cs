using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using ManufactPlanner.Models;
using ManufactPlanner.Views.Dialogs;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;

namespace ManufactPlanner.ViewModels
{
    public class OrdersViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private ObservableCollection<OrderItemViewModel> _orders;
        private ObservableCollection<OrderItemViewModel> _filteredOrders;
        private ObservableCollection<string> _statuses;
        private ObservableCollection<string> _customers;
        private ObservableCollection<string> _deliveryPeriods;
        private ObservableCollection<string> _creationPeriods;
        private string _selectedStatus;
        private string _selectedCustomer;
        private string _selectedDeliveryPeriod;
        private string _selectedCreationPeriod;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 10;
        private string _searchText = "";
        private bool _isLoading = false;

        public ObservableCollection<OrderItemViewModel> Orders
        {
            get => _filteredOrders;
            set => this.RaiseAndSetIfChanged(ref _filteredOrders, value);
        }

        public ObservableCollection<string> Statuses
        {
            get => _statuses;
            set => this.RaiseAndSetIfChanged(ref _statuses, value);
        }

        public ObservableCollection<string> Customers
        {
            get => _customers;
            set => this.RaiseAndSetIfChanged(ref _customers, value);
        }

        public ObservableCollection<string> DeliveryPeriods
        {
            get => _deliveryPeriods;
            set => this.RaiseAndSetIfChanged(ref _deliveryPeriods, value);
        }

        public ObservableCollection<string> CreationPeriods
        {
            get => _creationPeriods;
            set => this.RaiseAndSetIfChanged(ref _creationPeriods, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStatus, value);
                ApplyFilters();
            }
        }

        public string SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCustomer, value);
                ApplyFilters();
            }
        }

        public string SelectedDeliveryPeriod
        {
            get => _selectedDeliveryPeriod;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDeliveryPeriod, value);
                ApplyFilters();
            }
        }

        public string SelectedCreationPeriod
        {
            get => _selectedCreationPeriod;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCreationPeriod, value);
                ApplyFilters();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentPage, value);
                UpdatePaginatedOrders();
                this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                this.RaisePropertyChanged(nameof(CanGoToNextPage));
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => this.RaiseAndSetIfChanged(ref _totalPages, value);
        }

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                ApplyFilters();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public ICommand CreateOrderCommand { get; }
        public ICommand ShowOrderDetailsCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand GoToPageCommand { get; }
        public ICommand RefreshCommand { get; }

        public OrdersViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация команд с проверками CanExecute
            CreateOrderCommand = ReactiveCommand.Create(CreateOrder);
            ShowOrderDetailsCommand = ReactiveCommand.Create<int>(ShowOrderDetails);

            // Создаем команды с привязкой к CanGoToPreviousPage и CanGoToNextPage
            NextPageCommand = ReactiveCommand.Create(
                NextPage,
                this.WhenAnyValue(x => x.CanGoToNextPage));

            PreviousPageCommand = ReactiveCommand.Create(
                PreviousPage,
                this.WhenAnyValue(x => x.CanGoToPreviousPage));

            GoToPageCommand = ReactiveCommand.Create<int>(GoToPage);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadOrdersAsync);

            InitializeFilterOptions();

            // Асинхронная загрузка заказов при создании ViewModel
            System.Threading.Tasks.Task.Run(() => LoadOrdersAsync().ConfigureAwait(false));
        }

        private void InitializeFilterOptions()
        {
            _statuses = new ObservableCollection<string>
            {
                "Все статусы"
            };

            _customers = new ObservableCollection<string>
            {
                "Все заказчики"
            };

            _deliveryPeriods = new ObservableCollection<string>
            {
                "Любой период",
                "Этот месяц",
                "Следующий месяц",
                "Этот квартал",
                "Следующий квартал",
                "Просроченные"
            };

            _creationPeriods = new ObservableCollection<string>
            {
                "Любой период",
                "Сегодня",
                "На этой неделе",
                "В этом месяце",
                "В прошлом месяце"
            };

            _selectedStatus = "Все статусы";
            _selectedCustomer = "Все заказчики";
            _selectedDeliveryPeriod = "Любой период";
            _selectedCreationPeriod = "Любой период";

            // Инициализация списков для предотвращения NullReferenceException
            _orders = new ObservableCollection<OrderItemViewModel>();
            _filteredOrders = new ObservableCollection<OrderItemViewModel>();
        }

        private async System.Threading.Tasks.Task LoadOrdersAsync()
        {
            try
            {
                IsLoading = true;

                // Проверяем доступность базы данных
                if (_dbContext == null)
                {
                    // Если БД недоступна, используем тестовые данные
                    LoadTestData();
                    return;
                }

                // Получаем текущего пользователя из MainWindowViewModel
                Guid currentUserId = Guid.Empty;
                if (_mainWindowViewModel != null && _mainWindowViewModel.CurrentUserId != Guid.Empty)
                {
                    currentUserId = _mainWindowViewModel.CurrentUserId;
                }
                else
                {
                    // Если пользователь не определен, используем тестовые данные
                    LoadTestData();
                    return;
                }

                // Загружаем пользователя вместе с его ролями
                var user = await _dbContext.Users
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

                if (user == null)
                {
                    // Если пользователь не найден, используем тестовые данные
                    LoadTestData();
                    return;
                }

                // Получаем список ролей пользователя
                var userRoles = user.Roles.Select(r => r.Name).ToList();

                bool isAdmin = userRoles.Contains("Администратор");
                bool isManager = userRoles.Contains("Менеджер");

                // Загружаем заказы в зависимости от роли пользователя
                IQueryable<Order> ordersQuery;

                if (isAdmin)
                {
                    // Администратор видит все заказы
                    ordersQuery = _dbContext.Orders;
                }
                else if (isManager)
                {
                    // Менеджер видит заказы своего подразделения
                    var userDepartments = await _dbContext.UserDepartments
                        .Where(ud => ud.UserId == currentUserId)
                        .Select(ud => ud.DepartmentId)
                        .ToListAsync();

                    // Получаем заказы, где есть задачи для сотрудников из тех же подразделений, что и менеджер
                    ordersQuery = _dbContext.Orders.Where(o =>
                        o.OrderPositions.Any(op =>
                            op.Tasks.Any(t =>
                                t.Assignee.UserDepartments.Any(ud =>
                                    userDepartments.Contains(ud.DepartmentId)))));
                }
                else
                {
                    // Обычный исполнитель видит только свои заказы
                    ordersQuery = _dbContext.Orders.Where(o =>
                        o.OrderPositions.Any(op =>
                            op.Tasks.Any(t =>
                                t.AssigneeId == currentUserId)));
                }

                // Получаем все уникальные статусы заказов из БД
                var statusList = await _dbContext.Orders
                    .Select(o => o.Status)
                    .Where(s => s != null)
                    .Distinct()
                    .ToListAsync();

                Statuses.Clear();
                Statuses.Add("Все статусы");

                foreach (var status in statusList)
                {
                    Statuses.Add(status);
                }

                // Получаем всех уникальных заказчиков из БД
                var customerList = await _dbContext.Orders
                    .Select(o => o.Customer)
                    .Distinct()
                    .ToListAsync();

                Customers.Clear();
                Customers.Add("Все заказчики");

                foreach (var customer in customerList)
                {
                    Customers.Add(customer);
                }

                // Загружаем заказы с необходимыми связанными данными
                var orders = await ordersQuery
                    .Include(o => o.OrderPositions)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new OrderItemViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Name = o.Name,
                        Customer = o.Customer,
                        // Преобразуем DateOnly в строку для отображения
                        Deadline = o.DeliveryDeadline.HasValue ?
                            o.DeliveryDeadline.Value.ToString("dd.MM.yyyy") :
                            "-",
                        PositionsCount = o.OrderPositions.Count.ToString(),
                        Status = o.Status ?? "Неизвестно",
                        // Считаем срок критическим, если до него осталось меньше 7 дней
                        IsDateCritical = o.DeliveryDeadline.HasValue &&
                            o.DeliveryDeadline.Value <= DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync();

                _orders = new ObservableCollection<OrderItemViewModel>(orders);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке заказов: {ex.Message}");

                // В случае ошибки загружаем тестовые данные
                LoadTestData();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadTestData()
        {
            // Тестовые данные для отображения в случае ошибки или в режиме дизайнера
            var testOrders = new List<OrderItemViewModel>
            {
                new OrderItemViewModel { Id = 1, OrderNumber = "ОП-113/24", Name = "Институт развития проф. образования", Customer = "ИРПО", Deadline = "10.10.2024", PositionsCount = "1", Status = "Активен", CreatedAt = DateTime.Now.AddDays(-30) },
                new OrderItemViewModel { Id = 2, OrderNumber = "ОП-136/24", Name = "Колледж современных технологий", Customer = "КСТ", Deadline = "12.02.2025", PositionsCount = "12", Status = "Активен", IsDateCritical = true, CreatedAt = DateTime.Now.AddDays(-25) },
                new OrderItemViewModel { Id = 3, OrderNumber = "ОП-141/24", Name = "Губкинский горно-политехнический колледж", Customer = "ГГПК", Deadline = "14.10.2024", PositionsCount = "1", Status = "Активен", CreatedAt = DateTime.Now.AddDays(-20) },
                new OrderItemViewModel { Id = 4, OrderNumber = "ОП-145/24", Name = "ГГПК. Подземная разработка МПИ", Customer = "ГГПК", Deadline = "14.10.2024", PositionsCount = "2", Status = "Активен", CreatedAt = DateTime.Now.AddDays(-15) },
                new OrderItemViewModel { Id = 5, OrderNumber = "ОП-168/24", Name = "ГГПК. Электрооборудование", Customer = "ГГПК", Deadline = "17.02.2024", PositionsCount = "7", Status = "Активен", IsDateCritical = true, CreatedAt = DateTime.Now.AddDays(-10) },
                new OrderItemViewModel { Id = 6, OrderNumber = "ОП-169/24", Name = "АНО ПО \"МИКА\". Тренажер АРМ", Customer = "АНО ПО \"МИКА\"", Deadline = "01.05.2025", PositionsCount = "1", Status = "Активен", CreatedAt = DateTime.Now.AddDays(-5) },
                new OrderItemViewModel { Id = 7, OrderNumber = "ОП-170/24", Name = "Тестовый заказ 1", Customer = "ИРПО", Deadline = "01.06.2025", PositionsCount = "3", Status = "Завершен", CreatedAt = DateTime.Now.AddDays(-3) },
                new OrderItemViewModel { Id = 8, OrderNumber = "ОП-171/24", Name = "Тестовый заказ 2", Customer = "КСТ", Deadline = "15.06.2025", PositionsCount = "2", Status = "Отменен", CreatedAt = DateTime.Now.AddDays(-1) }
            };

            _orders = new ObservableCollection<OrderItemViewModel>(testOrders);

            // Заполняем фильтры на основе тестовых данных
            UpdateFilterOptionsFromTestData();

            ApplyFilters();
        }

        private void UpdateFilterOptionsFromTestData()
        {
            // Обновляем статусы
            var statuses = _orders.Select(o => o.Status).Distinct().ToList();
            Statuses.Clear();
            Statuses.Add("Все статусы");
            foreach (var status in statuses)
            {
                Statuses.Add(status);
            }

            // Обновляем заказчиков
            var customers = _orders.Select(o => o.Customer).Distinct().ToList();
            Customers.Clear();
            Customers.Add("Все заказчики");
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }

            // Сбрасываем выбранные фильтры к значениям по умолчанию
            SelectedStatus = "Все статусы";
            SelectedCustomer = "Все заказчики";
            SelectedDeliveryPeriod = "Любой период";
            SelectedCreationPeriod = "Любой период";
        }

        private void ApplyFilters()
        {
            if (_orders == null)
                return;

            IEnumerable<OrderItemViewModel> filteredOrders = _orders;

            // Фильтр по статусу
            if (!string.IsNullOrEmpty(_selectedStatus) && _selectedStatus != "Все статусы")
            {
                filteredOrders = filteredOrders.Where(o => o.Status == _selectedStatus);
            }

            // Фильтр по заказчику
            if (!string.IsNullOrEmpty(_selectedCustomer) && _selectedCustomer != "Все заказчики")
            {
                filteredOrders = filteredOrders.Where(o => o.Customer == _selectedCustomer);
            }

            // Фильтр по сроку поставки
            if (!string.IsNullOrEmpty(_selectedDeliveryPeriod) && _selectedDeliveryPeriod != "Любой период")
            {
                DateTime now = DateTime.Now;
                DateTime monthStart = new DateTime(now.Year, now.Month, 1);
                DateTime nextMonthStart = monthStart.AddMonths(1);
                DateTime nextMonthEnd = nextMonthStart.AddMonths(1).AddDays(-1);
                DateTime quarterStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
                DateTime nextQuarterStart = quarterStart.AddMonths(3);
                DateTime nextQuarterEnd = nextQuarterStart.AddMonths(3).AddDays(-1);

                switch (_selectedDeliveryPeriod)
                {
                    case "Этот месяц":
                        filteredOrders = filteredOrders.Where(o =>
                        {
                            if (o.Deadline != "-" && DateTime.TryParse(o.Deadline, out DateTime deadline))
                            {
                                return deadline >= monthStart && deadline < nextMonthStart;
                            }
                            return false;
                        });
                        break;
                    case "Следующий месяц":
                        filteredOrders = filteredOrders.Where(o =>
                        {
                            if (o.Deadline != "-" && DateTime.TryParse(o.Deadline, out DateTime deadline))
                            {
                                return deadline >= nextMonthStart && deadline < nextMonthEnd.AddDays(1);
                            }
                            return false;
                        });
                        break;
                    case "Этот квартал":
                        filteredOrders = filteredOrders.Where(o =>
                        {
                            if (o.Deadline != "-" && DateTime.TryParse(o.Deadline, out DateTime deadline))
                            {
                                return deadline >= quarterStart && deadline < nextQuarterStart;
                            }
                            return false;
                        });
                        break;
                    case "Следующий квартал":
                        filteredOrders = filteredOrders.Where(o =>
                        {
                            if (o.Deadline != "-" && DateTime.TryParse(o.Deadline, out DateTime deadline))
                            {
                                return deadline >= nextQuarterStart && deadline < nextQuarterEnd.AddDays(1);
                            }
                            return false;
                        });
                        break;
                    case "Просроченные":
                        filteredOrders = filteredOrders.Where(o => o.IsDateCritical);
                        break;
                }
            }

            // Фильтр по дате создания
            if (!string.IsNullOrEmpty(_selectedCreationPeriod) && _selectedCreationPeriod != "Любой период")
            {
                DateTime now = DateTime.Now;
                DateTime today = now.Date;
                DateTime weekStart = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday).Date;
                DateTime monthStart = new DateTime(now.Year, now.Month, 1);
                DateTime lastMonthStart = monthStart.AddMonths(-1);

                switch (_selectedCreationPeriod)
                {
                    case "Сегодня":
                        filteredOrders = filteredOrders.Where(o =>
                            o.CreatedAt.HasValue && o.CreatedAt.Value.Date == today);
                        break;
                    case "На этой неделе":
                        filteredOrders = filteredOrders.Where(o =>
                            o.CreatedAt.HasValue && o.CreatedAt.Value.Date >= weekStart);
                        break;
                    case "В этом месяце":
                        filteredOrders = filteredOrders.Where(o =>
                            o.CreatedAt.HasValue && o.CreatedAt.Value.Date >= monthStart);
                        break;
                    case "В прошлом месяце":
                        filteredOrders = filteredOrders.Where(o =>
                            o.CreatedAt.HasValue && o.CreatedAt.Value.Date >= lastMonthStart &&
                            o.CreatedAt.Value.Date < monthStart);
                        break;
                }
            }

            // Фильтр по поисковому запросу
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string search = _searchText.ToLower();
                filteredOrders = filteredOrders.Where(o =>
                    o.OrderNumber.ToLower().Contains(search) ||
                    o.Name.ToLower().Contains(search) ||
                    o.Customer.ToLower().Contains(search));
            }

            // Обновляем список и пагинацию
            _filteredOrders = new ObservableCollection<OrderItemViewModel>(filteredOrders);
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            // Вычисляем общее количество страниц
            TotalPages = Math.Max(1, (_filteredOrders.Count + PageSize - 1) / PageSize);

            // Корректируем текущую страницу, если она выходит за пределы
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
            else if (CurrentPage < 1)
                CurrentPage = 1;

            // Обновляем состояние кнопок пагинации
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
            this.RaisePropertyChanged(nameof(CanGoToNextPage));

            // Применяем пагинацию
            UpdatePaginatedOrders();
        }

        private void UpdatePaginatedOrders()
        {
            // Получаем только элементы для текущей страницы
            var paginatedOrders = _filteredOrders
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Создаем новую коллекцию для отображения
            var displayOrders = new ObservableCollection<OrderItemViewModel>(paginatedOrders);

            // Применение чередования цветов строк
            for (int i = 0; i < displayOrders.Count; i++)
            {
                displayOrders[i].IsAlternate = i % 2 == 1;
            }

            // Обновляем отображаемый список
            Orders = displayOrders;
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
            }
        }

        private async void CreateOrder()
        {
            try
            {
                // Получаем главное окно приложения
                var mainWindow = GetMainWindow();
                if (mainWindow == null)
                {
                    Console.WriteLine("Не удалось получить главное окно приложения");
                    return;
                }

                // Показываем диалог создания заказа
                var order = await OrderCreateDialog.ShowDialog(mainWindow, _dbContext);

                // Если заказ был создан, обновляем список заказов
                if (order != null)
                {
                    // Добавляем новый заказ в коллекцию с временным преобразованием в ViewModel
                    var newOrderVm = new OrderItemViewModel
                    {
                        Id = order.Id,
                        OrderNumber = order.OrderNumber,
                        Name = order.Name,
                        Customer = order.Customer,
                        Deadline = order.DeliveryDeadline.HasValue
                            ? order.DeliveryDeadline.Value.ToString("dd.MM.yyyy")
                            : "-",
                        PositionsCount = "0", // Новый заказ еще не имеет позиций
                        Status = order.Status ?? "Активен",
                        IsDateCritical = order.DeliveryDeadline.HasValue &&
                            order.DeliveryDeadline.Value <= DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                        CreatedAt = order.CreatedAt
                    };

                    // Добавляем новый заказ в начало списка
                    _orders.Insert(0, newOrderVm);

                    // Обновляем фильтры и пагинацию
                    ApplyFilters();

                    // Выводим уведомление об успешном создании заказа
                    Console.WriteLine($"Заказ {order.OrderNumber} успешно создан");

                    // Обновляем списки фильтров
                    if (!Customers.Contains(order.Customer))
                    {
                        Customers.Add(order.Customer);
                    }

                    if (!Statuses.Contains(order.Status) && !string.IsNullOrEmpty(order.Status))
                    {
                        Statuses.Add(order.Status);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании заказа: {ex.Message}");
                // Обработка ошибок
            }
        }

        private Window GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        private void ShowOrderDetails(int orderId)
        {
            // Переход на страницу с детальной информацией о заказе
            _mainWindowViewModel.NavigateToOrderDetails(orderId);
        }
    }

    public class OrderItemViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Name { get; set; }
        public string Customer { get; set; }
        public string Deadline { get; set; }
        public string PositionsCount { get; set; }
        public string Status { get; set; }
        private bool _isAlternate;
        public bool IsAlternate
        {
            get => _isAlternate;
            set => this.RaiseAndSetIfChanged(ref _isAlternate, value);
        }
        public bool IsDateCritical { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}