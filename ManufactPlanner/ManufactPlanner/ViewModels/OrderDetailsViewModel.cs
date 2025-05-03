using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class OrderDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private string _orderNumber = "";
        private string _orderName = "";
        private string _customer = "";
        private string _contractDeadline = "";
        private string _deliveryDeadline = "";
        private bool _isDeliveryDateCritical = false;
        private string _contractQuantity = "";
        private string _totalPrice = "";
        private string _status = "";
        private bool _isLoading = false;

        private ObservableCollection<OrderPositionViewModel> _positions;

        public string OrderNumber
        {
            get => _orderNumber;
            set => this.RaiseAndSetIfChanged(ref _orderNumber, value);
        }

        public string OrderName
        {
            get => _orderName;
            set => this.RaiseAndSetIfChanged(ref _orderName, value);
        }

        public string Customer
        {
            get => _customer;
            set => this.RaiseAndSetIfChanged(ref _customer, value);
        }

        public string ContractDeadline
        {
            get => _contractDeadline;
            set => this.RaiseAndSetIfChanged(ref _contractDeadline, value);
        }

        public string DeliveryDeadline
        {
            get => _deliveryDeadline;
            set => this.RaiseAndSetIfChanged(ref _deliveryDeadline, value);
        }

        public bool IsDeliveryDateCritical
        {
            get => _isDeliveryDateCritical;
            set => this.RaiseAndSetIfChanged(ref _isDeliveryDateCritical, value);
        }

        public string ContractQuantity
        {
            get => _contractQuantity;
            set => this.RaiseAndSetIfChanged(ref _contractQuantity, value);
        }

        public string TotalPrice
        {
            get => _totalPrice;
            set => this.RaiseAndSetIfChanged(ref _totalPrice, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public ObservableCollection<OrderPositionViewModel> Positions
        {
            get => _positions;
            set => this.RaiseAndSetIfChanged(ref _positions, value);
        }

        public ICommand NavigateToOrdersCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand AddPositionCommand { get; }
        public ICommand EditPositionCommand { get; }
        public ICommand CreateTaskCommand { get; }

        public OrderDetailsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int orderId)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            NavigateToOrdersCommand = ReactiveCommand.Create(NavigateToOrders);
            EditOrderCommand = ReactiveCommand.Create(EditOrder);
            AddPositionCommand = ReactiveCommand.Create(AddPosition);
            EditPositionCommand = ReactiveCommand.Create<int>(EditPosition);
            CreateTaskCommand = ReactiveCommand.Create<int>(CreateTask);

            // Инициализация пустой коллекции для предотвращения NullReferenceException
            _positions = new ObservableCollection<OrderPositionViewModel>();

            // Асинхронная загрузка данных
            System.Threading.Tasks.Task.Run(() => LoadOrderDetailsAsync(orderId));
        }

        private async System.Threading.Tasks.Task LoadOrderDetailsAsync(int orderId)
        {
            try
            {
                IsLoading = true;

                // Проверка доступности БД
                if (_dbContext == null)
                {
                    Console.WriteLine("База данных недоступна.");
                    return;
                }

                // Загружаем заказ со всеми связанными данными
                var order = await _dbContext.Orders
                    .Include(o => o.OrderPositions)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    Console.WriteLine($"Заказ с идентификатором {orderId} не найден.");
                    return;
                }

                // Заполняем свойства ViewModel данными из базы
                OrderNumber = order.OrderNumber;
                OrderName = order.Name;
                Customer = order.Customer;
                ContractDeadline = order.ContractDeadline?.ToString("dd.MM.yyyy") ?? "-";
                DeliveryDeadline = order.DeliveryDeadline?.ToString("dd.MM.yyyy") ?? "-";

                // Считаем срок критическим, если до него осталось меньше 7 дней
                IsDeliveryDateCritical = order.DeliveryDeadline.HasValue &&
                    order.DeliveryDeadline.Value <= DateOnly.FromDateTime(DateTime.Now.AddDays(7));

                ContractQuantity = $"{order.ContractQuantity} шт.";
                TotalPrice = $"{order.TotalPrice:N2} руб.";
                Status = order.Status ?? "Не указан";

                // Загружаем позиции заказа
                var positions = order.OrderPositions.Select((op, index) => new OrderPositionViewModel
                {
                    Id = op.Id,
                    PositionNumber = op.PositionNumber,
                    ProductName = op.ProductName,
                    Quantity = $"{op.Quantity} шт.",
                    Price = op.Price.HasValue ? $"{op.Price:N2} руб." : "0,00 руб.",
                    DevelopmentType = GetDevelopmentTypeName(op.DevelopmentType),
                    Status = GetStatusName(op.CurrentStatus),
                    StatusColor = GetStatusColor(op.CurrentStatus),
                    IsAlternate = index % 2 == 1 // Чередование строк
                }).ToList();

                // Обновляем коллекцию позиций в UI потоке
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Positions = new ObservableCollection<OrderPositionViewModel>(positions);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных заказа: {ex.Message}");

                // Здесь мы не загружаем тестовые данные, а просто логируем ошибку
                // и оставляем пустые значения или устанавливаем значения по умолчанию
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OrderNumber = "Ошибка загрузки";
                    Status = "Неизвестно";
                    Positions = new ObservableCollection<OrderPositionViewModel>();
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Вспомогательные методы для преобразования строк в читаемые значения
        private string GetDevelopmentTypeName(string type)
        {
            if (string.IsNullOrEmpty(type)) return "Не указан";

            return type switch
            {
                "Purchase" => "Покупное",
                "Development" => "Разработка",
                "Покупное" => "Покупное",
                "Разработка" => "Разработка",
                _ => type // Возвращаем как есть, если не знаем как преобразовать
            };
        }

        private string GetStatusName(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Не указан";

            return status switch
            {
                "Queue" => "В очереди",
                "InProgress" => "В процессе",
                "WaitingForProduction" => "Ждем производство",
                "Completed" => "Завершено",
                "В очереди" => "В очереди",
                "В процессе" => "В процессе",
                "Ждем производство" => "Ждем производство",
                "Завершено" => "Завершено",
                _ => status // Возвращаем как есть, если не знаем как преобразовать
            };
        }

        private string GetStatusColor(string status)
        {
            if (string.IsNullOrEmpty(status)) return "#9E9E9E"; // Серый для неизвестного статуса

            return status switch
            {
                "Queue" => "#9575CD", // Фиолетовый
                "InProgress" => "#00ACC1", // Голубой
                "WaitingForProduction" => "#FFB74D", // Оранжевый
                "Completed" => "#4CAF9D", // Зеленый
                "В очереди" => "#9575CD", // Фиолетовый
                "В процессе" => "#00ACC1", // Голубой
                "Ждем производство" => "#FFB74D", // Оранжевый
                "Завершено" => "#4CAF9D", // Зеленый
                _ => "#9E9E9E" // Серый
            };
        }

        private void NavigateToOrders()
        {
            _mainWindowViewModel.NavigateToOrders();
        }

        private void EditOrder()
        {
            // Логика редактирования заказа
            // В будущем здесь может быть код для открытия диалога редактирования
        }

        private void AddPosition()
        {
            // Логика добавления новой позиции в заказ
            // В будущем здесь может быть код для открытия диалога добавления позиции
        }

        private void EditPosition(int positionId)
        {
            // Логика редактирования позиции заказа
            // В будущем здесь может быть код для открытия диалога редактирования позиции
        }

        private void CreateTask(int positionId)
        {
            // Логика создания задачи для позиции заказа
            // В будущем здесь может быть код для открытия диалога создания задачи
        }
    }

    public class OrderPositionViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string PositionNumber { get; set; }
        public string ProductName { get; set; }
        public string Quantity { get; set; }
        public string Price { get; set; }
        public string DevelopmentType { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public bool IsAlternate { get; set; }
    }
}