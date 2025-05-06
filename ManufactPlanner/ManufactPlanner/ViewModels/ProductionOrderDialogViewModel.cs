using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.ViewModels.Dialogs
{
    public class ProductionOrderDialogViewModel : ViewModelBase
    {
        private readonly PostgresContext _dbContext;
        private Window _dialogWindow;
        private bool _dialogResult = false;

        // Свойства для списков и выбранных элементов
        private ObservableCollection<OrderListItem2> _orders;
        private ObservableCollection<OrderPositionListItem> _orderPositions;
        private OrderListItem2 _selectedOrder;
        private OrderPositionListItem _selectedOrderPosition;

        // Свойства формы
        private string _orderNumber = string.Empty;
        private string _masterName = string.Empty;
        private DateTime? _productionDate;
        private DateTime? _debuggingDate;
        private DateTime? _acceptanceDate;
        private DateTime? _packagingDate;
        private string _notes = string.Empty;

        // Свойства состояния
        private bool _isLoading = false;
        private string _statusMessage = string.Empty;
        private bool _showStatusMessage = false;
        private bool _hasError = false;

        // Публичные свойства
        public ObservableCollection<OrderListItem2> Orders
        {
            get => _orders;
            set => this.RaiseAndSetIfChanged(ref _orders, value);
        }

        public ObservableCollection<OrderPositionListItem> OrderPositions
        {
            get => _orderPositions;
            set
            {
                this.RaiseAndSetIfChanged(ref _orderPositions, value);
                this.RaisePropertyChanged(nameof(HasOrderPositions));
            }
        }

        public OrderListItem2 SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedOrder, value);
                this.RaisePropertyChanged(nameof(HasSelectedOrder));
                LoadOrderPositions();
            }
        }

        public OrderPositionListItem SelectedOrderPosition
        {
            get => _selectedOrderPosition;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedOrderPosition, value);
                this.RaisePropertyChanged(nameof(CanCreate));
                // Автоматически генерируем номер заказ-наряда при выборе позиции
                if (value != null && string.IsNullOrWhiteSpace(OrderNumber))
                {
                    GenerateOrderNumber();
                }
            }
        }

        public string OrderNumber
        {
            get => _orderNumber;
            set
            {
                this.RaiseAndSetIfChanged(ref _orderNumber, value);
                this.RaisePropertyChanged(nameof(CanCreate));
            }
        }

        public string MasterName
        {
            get => _masterName;
            set
            {
                this.RaiseAndSetIfChanged(ref _masterName, value);
                this.RaisePropertyChanged(nameof(CanCreate));
            }
        }

        public DateTime? ProductionDate
        {
            get => _productionDate;
            set => this.RaiseAndSetIfChanged(ref _productionDate, value);
        }

        public DateTime? DebuggingDate
        {
            get => _debuggingDate;
            set => this.RaiseAndSetIfChanged(ref _debuggingDate, value);
        }

        public DateTime? AcceptanceDate
        {
            get => _acceptanceDate;
            set => this.RaiseAndSetIfChanged(ref _acceptanceDate, value);
        }

        public DateTime? PackagingDate
        {
            get => _packagingDate;
            set => this.RaiseAndSetIfChanged(ref _packagingDate, value);
        }

        public string Notes
        {
            get => _notes;
            set => this.RaiseAndSetIfChanged(ref _notes, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _statusMessage, value);
                ShowStatusMessage = !string.IsNullOrEmpty(value);
            }
        }

        public bool ShowStatusMessage
        {
            get => _showStatusMessage;
            set => this.RaiseAndSetIfChanged(ref _showStatusMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => this.RaiseAndSetIfChanged(ref _hasError, value);
        }

        // Вычисляемые свойства
        public bool HasSelectedOrder => SelectedOrder != null;
        public bool HasOrderPositions => OrderPositions != null && OrderPositions.Count > 0;
        public bool CanCreate => HasSelectedOrder &&
                                SelectedOrderPosition != null &&
                                !string.IsNullOrWhiteSpace(OrderNumber) &&
                                !string.IsNullOrWhiteSpace(MasterName);

        public bool DialogResult => _dialogResult;

        // Команды
        public ReactiveCommand<Unit, Unit> RefreshOrdersCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public ProductionOrderDialogViewModel(PostgresContext dbContext)
        {
            _dbContext = dbContext;

            // Инициализируем пустые коллекции
            Orders = new ObservableCollection<OrderListItem2>();
            OrderPositions = new ObservableCollection<OrderPositionListItem>();

            // Инициализация команд
            RefreshOrdersCommand = ReactiveCommand.CreateFromTask(LoadOrdersAsync);
            ConfirmCommand = ReactiveCommand.CreateFromTask(CreateProductionOrderAsync);
            CancelCommand = ReactiveCommand.Create(Cancel);

            // Загрузка данных
            _ = LoadOrdersAsync();

            // Установка текущей даты по умолчанию
            ProductionDate = DateTime.Now;
        }

        // Метод для ссылки на диалоговое окно
        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        // Загрузка списка заказов
        private async System.Threading.Tasks.Task LoadOrdersAsync()
        {
            if (_dbContext == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка заказов...";
                HasError = false;

                var orders = await _dbContext.Orders
                    .Where(o => o.Status == "Активен") // Загружаем только активные заказы
                    .OrderByDescending(o => o.Id)
                    .Select(o => new OrderListItem2
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Name = o.Name,
                        DisplayText = $"{o.OrderNumber} - {o.Name}"
                    })
                    .ToListAsync();

                Orders = new ObservableCollection<OrderListItem2>(orders);

                if (orders.Count == 0)
                {
                    StatusMessage = "Заказы не найдены";
                    HasError = true;
                }
                else
                {
                    StatusMessage = $"Загружено заказов: {orders.Count}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки заказов: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Загрузка позиций выбранного заказа
        // Исправление метода загрузки позиций заказа
        private async void LoadOrderPositions()
        {
            if (_dbContext == null || SelectedOrder == null)
            {
                // Если нет контекста БД или не выбран заказ, очищаем список позиций
                OrderPositions = new ObservableCollection<OrderPositionListItem>();
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка позиций заказа...";
                HasError = false;

                // Добавим логирование для отладки
                Console.WriteLine($"Загрузка позиций для заказа ID: {SelectedOrder.Id}");

                // Явно указываем загрузку позиций для выбранного заказа
                var positions = await _dbContext.OrderPositions
                    .Where(op => op.OrderId == SelectedOrder.Id)
                    .OrderBy(op => op.PositionNumber)
                    .Select(op => new OrderPositionListItem
                    {
                        Id = op.Id,
                        PositionNumber = op.PositionNumber,
                        ProductName = op.ProductName,
                        DisplayText = $"{op.PositionNumber} - {op.ProductName}"
                    })
                    .ToListAsync();

                // Проверяем результат запроса
                Console.WriteLine($"Загружено позиций: {positions.Count}");

                // Обновляем коллекцию в основном потоке
                OrderPositions = new ObservableCollection<OrderPositionListItem>(positions);

                if (positions.Count == 0)
                {
                    StatusMessage = "Позиции заказа не найдены";
                    HasError = true;
                }
                else
                {
                    StatusMessage = $"Загружено позиций: {positions.Count}";
                    HasError = false;
                }
            }
            catch (Exception ex)
            {
                // Расширенное логирование ошибки
                Console.WriteLine($"Ошибка загрузки позиций заказа: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                StatusMessage = $"Ошибка загрузки позиций заказа: {ex.Message}";
                HasError = true;
                OrderPositions = new ObservableCollection<OrderPositionListItem>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Генерация номера заказ-наряда
        private void GenerateOrderNumber()
        {
            if (SelectedOrder != null && SelectedOrderPosition != null)
            {
                // Формат заказ-наряда: "ЗН-{текущий год последние 2 цифры}-{номер заказа}-{номер позиции}"
                string year = DateTime.Now.Year.ToString().Substring(2);
                string orderNumber = SelectedOrder.OrderNumber.Replace("ОП-", "").Split('/')[0];
                string positionNumber = SelectedOrderPosition.PositionNumber.Replace(".", "-");

                OrderNumber = $"ЗН-{year}-{orderNumber}-{positionNumber}";
            }
        }

        // Создание заказ-наряда
        private async System.Threading.Tasks.Task CreateProductionOrderAsync()
        {
            if (_dbContext == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Создание заказ-наряда...";
                HasError = false;

                if (!ValidateForm())
                {
                    return;
                }

                // Проверка на существование заказ-наряда с таким же номером
                var existingOrder = await _dbContext.ProductionDetails
                    .FirstOrDefaultAsync(p => p.OrderNumber == OrderNumber);

                if (existingOrder != null)
                {
                    StatusMessage = "Заказ-наряд с таким номером уже существует";
                    HasError = true;
                    return;
                }

                // Создание нового заказ-наряда
                var productionOrder = new ProductionDetail
                {
                    OrderPositionId = SelectedOrderPosition.Id,
                    OrderNumber = OrderNumber,
                    MasterName = MasterName,
                    ProductionDate = ProductionDate.HasValue ? DateOnly.FromDateTime(ProductionDate.Value) : null,
                    DebuggingDate = DebuggingDate.HasValue ? DateOnly.FromDateTime(DebuggingDate.Value) : null,
                    AcceptanceDate = AcceptanceDate.HasValue ? DateOnly.FromDateTime(AcceptanceDate.Value) : null,
                    PackagingDate = PackagingDate.HasValue ? DateOnly.FromDateTime(PackagingDate.Value) : null,
                    Notes = Notes,
                    UpdatedAt = DateTime.Now
                };

                // Добавление и сохранение в БД
                _dbContext.ProductionDetails.Add(productionOrder);
                await _dbContext.SaveChangesAsync();

                StatusMessage = "Заказ-наряд успешно создан";
                _dialogResult = true;

                // Закрытие диалога через некоторое время
                await System.Threading.Tasks.Task.Delay(1000);
                _dialogWindow?.Close();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка создания заказ-наряда: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Валидация формы
        private bool ValidateForm()
        {
            if (SelectedOrder == null)
            {
                StatusMessage = "Выберите заказ";
                HasError = true;
                return false;
            }

            if (SelectedOrderPosition == null)
            {
                StatusMessage = "Выберите позицию заказа";
                HasError = true;
                return false;
            }

            if (string.IsNullOrWhiteSpace(OrderNumber))
            {
                StatusMessage = "Введите номер заказ-наряда";
                HasError = true;
                return false;
            }

            if (string.IsNullOrWhiteSpace(MasterName))
            {
                StatusMessage = "Введите ФИО мастера";
                HasError = true;
                return false;
            }

            // Проверка последовательности дат
            if (ProductionDate.HasValue && DebuggingDate.HasValue && ProductionDate > DebuggingDate)
            {
                StatusMessage = "Дата изготовления не может быть позже даты отладки";
                HasError = true;
                return false;
            }

            if (DebuggingDate.HasValue && AcceptanceDate.HasValue && DebuggingDate > AcceptanceDate)
            {
                StatusMessage = "Дата отладки не может быть позже даты приемки";
                HasError = true;
                return false;
            }

            if (AcceptanceDate.HasValue && PackagingDate.HasValue && AcceptanceDate > PackagingDate)
            {
                StatusMessage = "Дата приемки не может быть позже даты упаковки";
                HasError = true;
                return false;
            }

            return true;
        }

        // Отмена диалога
        private void Cancel()
        {
            _dialogResult = false;
            _dialogWindow?.Close();
        }
    }

    // Классы для списков выбора
    public class OrderListItem2
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Name { get; set; }
        public string DisplayText { get; set; }
    }

    public class OrderPositionListItem
    {
        public int Id { get; set; }
        public string PositionNumber { get; set; }
        public string ProductName { get; set; }
        public string DisplayText { get; set; }
    }
}