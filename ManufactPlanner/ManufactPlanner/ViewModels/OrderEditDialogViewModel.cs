// ViewModels/Dialogs/OrderEditDialogViewModel.cs
using System;
using System.Reactive;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.ViewModels.Dialogs
{
    public class OrderEditDialogViewModel : ViewModelBase
    {
        private readonly PostgresContext _dbContext;
        private Window _dialogWindow;

        // Данные заказа
        private int _orderId;
        private string _orderNumber;
        private string _orderName;
        private string _customer;
        private DateOnly? _contractDeadline;
        private DateOnly? _deliveryDeadline;
        private DateOnly? _shippingDate;
        private int _contractQuantity = 1;
        private decimal? _totalPrice;
        private string _status = "Активен";
        private bool _hasInstallation;
        private bool _isEditMode;
        private bool _isLoading = false;
        private string _errorMessage;
        private bool _hasError;

        // Возможные статусы заказа
        private List<string> _possibleStatuses = new List<string>
        {
            "Активен",
            "Завершен",
            "Отменен",
            "На паузе",
            "Черновик"
        };

        // Свойства для связывания с UI
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

        public bool HasInstallation
        {
            get => _hasInstallation;
            set => this.RaiseAndSetIfChanged(ref _hasInstallation, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => this.RaiseAndSetIfChanged(ref _isEditMode, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public List<string> PossibleStatuses
        {
            get => _possibleStatuses;
            set => this.RaiseAndSetIfChanged(ref _possibleStatuses, value);
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

        // Команды для кнопок
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Конструктор для создания нового заказа
        public OrderEditDialogViewModel(PostgresContext dbContext)
        {
            _dbContext = dbContext;
            _isEditMode = false;
            OrderNumber = GenerateNewOrderNumber();
            Status = "Активен";
            ContractQuantity = 1;

            SaveCommand = ReactiveCommand.CreateFromTask(SaveOrderAsync);
            CancelCommand = ReactiveCommand.Create(CancelEdit);
        }

        // Конструктор для редактирования существующего заказа
        public OrderEditDialogViewModel(PostgresContext dbContext, int orderId)
        {
            _dbContext = dbContext;
            _orderId = orderId;
            _isEditMode = true;

            SaveCommand = ReactiveCommand.CreateFromTask(SaveOrderAsync);
            CancelCommand = ReactiveCommand.Create(CancelEdit);

            // Загружаем данные заказа
            System.Threading.Tasks.Task.Run(() => LoadOrderDataAsync());
        }

        // Метод для установки ссылки на диалоговое окно
        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        // Метод для загрузки данных заказа
        private async System.Threading.Tasks.Task LoadOrderDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var order = await _dbContext.Orders.FindAsync(_orderId);
                if (order == null)
                {
                    ErrorMessage = "Заказ не найден";
                    return;
                }

                // Загружаем данные заказа в UI поток
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OrderNumber = order.OrderNumber;
                    OrderName = order.Name;
                    Customer = order.Customer;
                    ContractDeadline = order.ContractDeadline;
                    DeliveryDeadline = order.DeliveryDeadline;
                    ShippingDate = order.ShippingDate;
                    ContractQuantity = order.ContractQuantity;
                    TotalPrice = order.TotalPrice;
                    Status = order.Status ?? "Активен";
                    HasInstallation = order.HasInstallation ?? false;
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при загрузке данных: {ex.Message}";
                Console.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Метод для сохранения заказа
        private async System.Threading.Tasks.Task SaveOrderAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Валидация данных
                if (string.IsNullOrWhiteSpace(OrderNumber))
                {
                    ErrorMessage = "Номер заказа не может быть пустым";
                    return;
                }

                if (string.IsNullOrWhiteSpace(OrderName))
                {
                    ErrorMessage = "Название заказа не может быть пустым";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Customer))
                {
                    ErrorMessage = "Заказчик не может быть пустым";
                    return;
                }

                // Проверяем уникальность номера заказа для новых заказов
                if (!IsEditMode)
                {
                    var existingOrder = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == OrderNumber);
                    if (existingOrder != null)
                    {
                        ErrorMessage = "Заказ с таким номером уже существует";
                        return;
                    }
                }

                // Редактируем существующий или создаем новый заказ
                Order order;
                if (IsEditMode)
                {
                    order = await _dbContext.Orders.FindAsync(_orderId);
                    if (order == null)
                    {
                        ErrorMessage = "Заказ не найден";
                        return;
                    }
                }
                else
                {
                    order = new Order
                    {
                        CreatedAt = DateTime.Now,
                        CreatedBy = _dbContext.Users.FirstOrDefault()?.Id // Тут должен быть ID текущего пользователя
                    };
                    _dbContext.Orders.Add(order);
                }

                // Обновляем данные заказа
                order.OrderNumber = OrderNumber;
                order.Name = OrderName;
                order.Customer = Customer;
                order.ContractDeadline = ContractDeadline;
                order.DeliveryDeadline = DeliveryDeadline;
                order.ShippingDate = ShippingDate;
                order.ContractQuantity = ContractQuantity;
                order.TotalPrice = TotalPrice;
                order.Status = Status;
                order.HasInstallation = HasInstallation;
                order.UpdatedAt = DateTime.Now;

                // Сохраняем изменения
                await _dbContext.SaveChangesAsync();

                // Закрываем окно с результатом
                _dialogWindow?.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                Console.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Отмена редактирования
        private void CancelEdit()
        {
            _dialogWindow?.Close(false);
        }

        // Генератор номера нового заказа
        private string GenerateNewOrderNumber()
        {
            var year = DateTime.Now.Year.ToString().Substring(2);
            var lastOrder = _dbContext.Orders
                .Where(o => o.OrderNumber.EndsWith("/" + year))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefault();

            if (lastOrder != null)
            {
                // Извлекаем номер из формата "ОП-123/24"
                string[] parts = lastOrder.OrderNumber.Split('-', '/');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int number))
                {
                    return $"ОП-{number + 1}/{year}";
                }
            }

            // Если не удалось найти или разобрать последний заказ, начинаем с 1
            return $"ОП-1/{year}";
        }
    }
}