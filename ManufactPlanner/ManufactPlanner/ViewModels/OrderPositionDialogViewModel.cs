// ViewModels/Dialogs/OrderPositionDialogViewModel.cs
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.ViewModels.Dialogs
{
    public class OrderPositionDialogViewModel : ViewModelBase
    {
        private readonly PostgresContext _dbContext;
        private Window _dialogWindow;

        // Данные позиции заказа
        private int _id;
        private int _orderId;
        private string _orderNumber;
        private string _positionNumber;
        private string _productName;
        private int _quantity = 1;
        private decimal? _price;
        private decimal? _totalPrice;
        private string _developmentType = "Разработка";
        private string _workflowTime = "2-3 дня";
        private string _currentStatus = "В очереди";
        private bool _isEditMode;
        private bool _isLoading = false;
        private string _errorMessage;
        private bool _hasError;
        private string _windowTitle = "Добавление позиции заказа";

        // Списки возможных значений
        private List<string> _developmentTypes = new List<string>
        {
            "Разработка",
            "Покупное",
            "Подрядчик"
        };

        private List<string> _workflowTimes = new List<string>
        {
            "1 день",
            "2-3 дня",
            "4-7 дней",
            "Более недели"
        };

        private List<string> _statuses = new List<string>
        {
            "В очереди",
            "В процессе",
            "Ждем производство",
            "Завершено"
        };

        // Свойства для связывания с UI
        public string WindowTitle
        {
            get => _windowTitle;
            set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
        }

        public string OrderNumber
        {
            get => _orderNumber;
            set => this.RaiseAndSetIfChanged(ref _orderNumber, value);
        }

        public string PositionNumber
        {
            get => _positionNumber;
            set => this.RaiseAndSetIfChanged(ref _positionNumber, value);
        }

        public string ProductName
        {
            get => _productName;
            set => this.RaiseAndSetIfChanged(ref _productName, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                this.RaiseAndSetIfChanged(ref _quantity, value);
                CalculateTotalPrice();
            }
        }

        public decimal? Price
        {
            get => _price;
            set
            {
                this.RaiseAndSetIfChanged(ref _price, value);
                CalculateTotalPrice();
            }
        }

        public decimal? TotalPrice
        {
            get => _totalPrice;
            set => this.RaiseAndSetIfChanged(ref _totalPrice, value);
        }

        public string DevelopmentType
        {
            get => _developmentType;
            set => this.RaiseAndSetIfChanged(ref _developmentType, value);
        }

        public string WorkflowTime
        {
            get => _workflowTime;
            set => this.RaiseAndSetIfChanged(ref _workflowTime, value);
        }

        public string CurrentStatus
        {
            get => _currentStatus;
            set => this.RaiseAndSetIfChanged(ref _currentStatus, value);
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

        public List<string> DevelopmentTypes
        {
            get => _developmentTypes;
            set => this.RaiseAndSetIfChanged(ref _developmentTypes, value);
        }

        public List<string> WorkflowTimes
        {
            get => _workflowTimes;
            set => this.RaiseAndSetIfChanged(ref _workflowTimes, value);
        }

        public List<string> Statuses
        {
            get => _statuses;
            set => this.RaiseAndSetIfChanged(ref _statuses, value);
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

        // Конструктор для создания новой позиции
        public OrderPositionDialogViewModel(PostgresContext dbContext, int orderId)
        {
            _dbContext = dbContext;
            _orderId = orderId;
            _isEditMode = false;

            SaveCommand = ReactiveCommand.CreateFromTask(SavePositionAsync);
            CancelCommand = ReactiveCommand.Create(CancelEdit);

            // Загрузим номер заказа для отображения
            LoadOrderNumber();
        }

        // Конструктор для редактирования существующей позиции
        public OrderPositionDialogViewModel(PostgresContext dbContext, double positionId)
        {
            _dbContext = dbContext;
            _id = (int)positionId;
            _isEditMode = true;
            WindowTitle = "Редактирование позиции заказа";

            SaveCommand = ReactiveCommand.CreateFromTask(SavePositionAsync);
            CancelCommand = ReactiveCommand.Create(CancelEdit);

            // Загружаем данные позиции
            System.Threading.Tasks.Task.Run(() => LoadPositionDataAsync());
        }

        // Метод для установки ссылки на диалоговое окно
        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        // Метод для загрузки номера заказа
        private void LoadOrderNumber()
        {
            try
            {
                var order = _dbContext.Orders.FirstOrDefault(o => o.Id == _orderId);
                if (order != null)
                {
                    OrderNumber = order.OrderNumber;

                    // Формируем следующий номер позиции
                    var existingPositions = _dbContext.OrderPositions
                        .Where(p => p.OrderId == _orderId)
                        .OrderByDescending(p => p.PositionNumber)
                        .Select(p => p.PositionNumber)
                        .ToList();

                    if (existingPositions.Any())
                    {
                        // Найдем последний номер позиции и увеличим его
                        var lastPosition = existingPositions.First();
                        if (decimal.TryParse(lastPosition, out decimal posNum))
                        {
                            PositionNumber = (posNum + 1).ToString();
                        }
                        else if (lastPosition.Contains('.'))
                        {
                            // Формат вида "1.1", "1.2" и т.д.
                            var parts = lastPosition.Split('.');
                            if (parts.Length == 2 && decimal.TryParse(parts[1], out decimal subNum))
                            {
                                PositionNumber = $"{parts[0]}.{subNum + 1}";
                            }
                            else
                            {
                                PositionNumber = "1";
                            }
                        }
                        else
                        {
                            PositionNumber = "1";
                        }
                    }
                    else
                    {
                        // Если позиций нет, начнем с 1
                        PositionNumber = "1";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке номера заказа: {ex.Message}");
                ErrorMessage = "Не удалось загрузить номер заказа";
            }
        }

        // Метод для загрузки данных позиции
        private async System.Threading.Tasks.Task LoadPositionDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var position = await _dbContext.OrderPositions
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == _id);

                if (position == null)
                {
                    ErrorMessage = "Позиция не найдена";
                    return;
                }

                // Загружаем данные позиции в UI поток
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _orderId = position.OrderId ?? 0;
                    OrderNumber = position.Order?.OrderNumber ?? "Н/Д";
                    PositionNumber = position.PositionNumber;
                    ProductName = position.ProductName;
                    Quantity = position.Quantity;
                    Price = position.Price;
                    TotalPrice = position.TotalPrice;
                    DevelopmentType = position.DevelopmentType ?? "Разработка";
                    WorkflowTime = position.WorkflowTime ?? "2-3 дня";
                    CurrentStatus = position.CurrentStatus ?? "В очереди";
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

        // Автоматический расчет общей суммы
        private void CalculateTotalPrice()
        {
            if (Price.HasValue)
            {
                TotalPrice = Price.Value * Quantity;
            }
            else
            {
                TotalPrice = null;
            }
        }

        // Метод для сохранения позиции
        private async System.Threading.Tasks.Task SavePositionAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Валидация данных
                if (string.IsNullOrWhiteSpace(PositionNumber))
                {
                    ErrorMessage = "Номер позиции не может быть пустым";
                    return;
                }

                if (string.IsNullOrWhiteSpace(ProductName))
                {
                    ErrorMessage = "Название продукта не может быть пустым";
                    return;
                }

                if (Quantity <= 0)
                {
                    ErrorMessage = "Количество должно быть больше нуля";
                    return;
                }

                // Проверяем уникальность номера позиции для добавления
                if (!IsEditMode)
                {
                    var existingPosition = await _dbContext.OrderPositions
                        .FirstOrDefaultAsync(p => p.OrderId == _orderId && p.PositionNumber == PositionNumber);

                    if (existingPosition != null)
                    {
                        ErrorMessage = "Позиция с таким номером уже существует в этом заказе";
                        return;
                    }
                }

                // Редактируем существующую или создаем новую позицию
                OrderPosition position;
                if (IsEditMode)
                {
                    position = await _dbContext.OrderPositions.FindAsync(_id);
                    if (position == null)
                    {
                        ErrorMessage = "Позиция не найдена";
                        return;
                    }
                }
                else
                {
                    position = new OrderPosition
                    {
                        OrderId = _orderId,
                        CreatedAt = DateTime.Now
                    };
                    _dbContext.OrderPositions.Add(position);
                }

                // Обновляем данные позиции
                position.PositionNumber = PositionNumber;
                position.ProductName = ProductName;
                position.Quantity = Quantity;
                position.Price = Price;
                position.TotalPrice = TotalPrice;
                position.DevelopmentType = DevelopmentType;
                position.WorkflowTime = WorkflowTime;
                position.CurrentStatus = CurrentStatus;
                position.UpdatedAt = DateTime.Now;

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
    }
}