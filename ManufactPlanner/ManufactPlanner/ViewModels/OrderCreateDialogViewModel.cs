using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class OrderCreateDialogViewModel : ViewModelBase
    {
        private readonly PostgresContext _dbContext;
        private string _orderNumber;
        private string _name;
        private string _customer;
        private bool _hasInstallation;
        private DateOnly? _contractDeadline;
        private DateOnly? _deliveryDeadline;
        private DateOnly? _shippingDate;
        private int _contractQuantity = 1;
        private decimal? _totalPrice;
        private string _status = "Активен";
        private bool _isProcessing;
        private string _errorMessage;
        private bool _hasError;
        private ObservableCollection<string> _customers;
        private string _selectedCustomer;

        // Свойства с уведомлением об изменении
        public string OrderNumber
        {
            get => _orderNumber;
            set
            {
                this.RaiseAndSetIfChanged(ref _orderNumber, value);
                ValidateOrderNumber();
            }
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string Customer
        {
            get => _customer;
            set => this.RaiseAndSetIfChanged(ref _customer, value);
        }

        public bool HasInstallation
        {
            get => _hasInstallation;
            set => this.RaiseAndSetIfChanged(ref _hasInstallation, value);
        }

        public DateOnly? ContractDeadline
        {
            get => _contractDeadline;
            set => this.RaiseAndSetIfChanged(ref _contractDeadline, value);
        }

        public DateOnly? DeliveryDeadline
        {
            get => _deliveryDeadline;
            set => this.RaiseAndSetIfChanged(ref _deliveryDeadline, value);
        }

        public DateOnly? ShippingDate
        {
            get => _shippingDate;
            set => this.RaiseAndSetIfChanged(ref _shippingDate, value);
        }

        public int ContractQuantity
        {
            get => _contractQuantity;
            set => this.RaiseAndSetIfChanged(ref _contractQuantity, value);
        }

        public decimal? TotalPrice
        {
            get => _totalPrice;
            set => this.RaiseAndSetIfChanged(ref _totalPrice, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _hasError;
            set => this.RaiseAndSetIfChanged(ref _hasError, value);
        }

        public ObservableCollection<string> Customers
        {
            get => _customers;
            set => this.RaiseAndSetIfChanged(ref _customers, value);
        }

        public string SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCustomer, value);
                if (!string.IsNullOrEmpty(value))
                {
                    Customer = value;
                }
            }
        }

        // Команды диалогового окна
        public ReactiveCommand<Unit, (bool Success, Order Order)> SaveCommand { get; }
        public ReactiveCommand<Unit, (bool Success, Order Order)> CancelCommand { get; }

        // Опции статуса заказа
        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>
        {
            "Активен",
            "В обработке",
            "Выполнен",
            "Отменен"
        };

        public OrderCreateDialogViewModel(PostgresContext dbContext)
        {
            _dbContext = dbContext;

            // Автоматическая генерация номера заказа
            GenerateOrderNumber();

            // Заполнение списка заказчиков
            LoadCustomers();

            // Инициализация команд
            SaveCommand = ReactiveCommand.CreateFromTask(SaveOrderAsync);
            CancelCommand = ReactiveCommand.Create(() => (false, (Order)null));
        }

        private void GenerateOrderNumber()
        {
            try
            {
                // Получаем текущий год
                string year = DateTime.Now.Year.ToString().Substring(2);

                // Пытаемся найти последний номер заказа в этом году
                var lastOrderNumber = _dbContext.Orders
                    .Where(o => o.OrderNumber.EndsWith("/" + year))
                    .OrderByDescending(o => o.OrderNumber)
                    .Select(o => o.OrderNumber)
                    .FirstOrDefault();

                int orderSequence = 1;

                if (lastOrderNumber != null)
                {
                    // Разбираем номер заказа (формат: "ОП-###/YY")
                    var parts = lastOrderNumber.Split('-', '/');
                    if (parts.Length >= 2 && int.TryParse(parts[1].Split('/')[0], out int lastSequence))
                    {
                        orderSequence = lastSequence + 1;
                    }
                }

                // Формируем новый номер заказа
                OrderNumber = $"ОП-{orderSequence}/{year}";
            }
            catch (Exception ex)
            {
                // В случае ошибки создаем временный номер
                OrderNumber = $"ОП-NEW/{DateTime.Now.Year.ToString().Substring(2)}";
                Console.WriteLine($"Ошибка при генерации номера заказа: {ex.Message}");
            }
        }

        private async void LoadCustomers()
        {
            try
            {
                // Получаем список уникальных заказчиков из существующих заказов
                var customersList = await System.Threading.Tasks.Task.Run(() =>
                    _dbContext.Orders
                        .Select(o => o.Customer)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList());

                Customers = new ObservableCollection<string>(customersList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке списка заказчиков: {ex.Message}");
                Customers = new ObservableCollection<string>();
            }
        }

        private void ValidateOrderNumber()
        {
            if (string.IsNullOrWhiteSpace(OrderNumber))
            {
                ErrorMessage = "Номер заказа не может быть пустым";
                return;
            }

            // Проверка формата номера заказа (ОП-###/YY)
            if (!OrderNumber.StartsWith("ОП-") || !OrderNumber.Contains('/'))
            {
                ErrorMessage = "Неверный формат номера заказа. Используйте формат ОП-XXX/YY";
                return;
            }

            ErrorMessage = string.Empty;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(OrderNumber))
            {
                ErrorMessage = "Номер заказа не может быть пустым";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Название заказа не может быть пустым";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Customer))
            {
                ErrorMessage = "Заказчик не может быть пустым";
                return false;
            }

            if (ContractQuantity <= 0)
            {
                ErrorMessage = "Количество должно быть больше 0";
                return false;
            }

            // Проверка дат
            if (DeliveryDeadline.HasValue && ContractDeadline.HasValue &&
                DeliveryDeadline.Value > ContractDeadline.Value)
            {
                ErrorMessage = "Срок поставки не может быть позже срока по договору";
                return false;
            }

            // Проверка уникальности номера заказа
            if (_dbContext.Orders.Any(o => o.OrderNumber == OrderNumber))
            {
                ErrorMessage = "Заказ с таким номером уже существует";
                return false;
            }

            return true;
        }

        private async Task<(bool Success, Order Order)> SaveOrderAsync()
        {
            if (!ValidateInput())
            {
                return (false, null);
            }

            try
            {
                IsProcessing = true;
                ErrorMessage = string.Empty;

                // Создаем новый заказ
                var order = new Order
                {
                    OrderNumber = OrderNumber,
                    Name = Name,
                    Customer = Customer,
                    HasInstallation = HasInstallation,
                    ContractDeadline = ContractDeadline,
                    DeliveryDeadline = DeliveryDeadline,
                    ShippingDate = ShippingDate,
                    ContractQuantity = ContractQuantity,
                    TotalPrice = TotalPrice,
                    Status = Status,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Сохраняем в базу данных
                await System.Threading.Tasks.Task.Run(() =>
                {
                    _dbContext.Orders.Add(order);
                    _dbContext.SaveChanges();
                });

                // Если выбранный заказчик еще не существует в таблице Customer, добавляем его
                if (!string.IsNullOrEmpty(Customer) &&
                    !_dbContext.Customers.Any(c => c.Name == Customer))
                {
                    var newCustomer = new Customer
                    {
                        Name = Customer,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        _dbContext.Customers.Add(newCustomer);
                        _dbContext.SaveChanges();

                        // Связываем заказ с заказчиком в таблице order_customers
                        _dbContext.Database.ExecuteSqlRaw(
                            "INSERT INTO order_customers (order_id, customer_id) VALUES ({0}, {1})",
                            order.Id, newCustomer.Id);
                    });
                }
                else if (!string.IsNullOrEmpty(Customer))
                {
                    // Связываем заказ с существующим заказчиком
                    var existingCustomer = await System.Threading.Tasks.Task.Run(() =>
                        _dbContext.Customers.FirstOrDefault(c => c.Name == Customer));

                    if (existingCustomer != null)
                    {
                        await System.Threading.Tasks.Task.Run(() =>
                        {
                            _dbContext.Database.ExecuteSqlRaw(
                                "INSERT INTO order_customers (order_id, customer_id) VALUES ({0}, {1})",
                                order.Id, existingCustomer.Id);
                        });
                    }
                }

                return (true, order);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении заказа: {ex.Message}";
                return (false, null);
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}