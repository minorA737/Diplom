using Avalonia.Controls;
using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ManufactPlanner.Views.Dialogs;

namespace ManufactPlanner.ViewModels
{
    public class OrderDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private Window _parentWindow;

        private int _orderId;
        private string _orderNumber = "";
        private string _orderName = "";
        private string _customer = "";
        private string _contractDeadline = "";
        private string _deliveryDeadline = "";
        private bool _isDeliveryDateCritical = false;
        private string _contractQuantity = "";
        private string _totalPrice = "";
        private string _status = "";
        private bool _isLoading = true;
        private string _activeTab = "positions"; // Текущая активная вкладка: positions, documentation, deadlines

        private ObservableCollection<OrderPositionViewModel> _positions;
        private ObservableCollection<DocumentationViewModel> _documentations;
        private ObservableCollection<DeadlineViewModel> _deadlines;

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

        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                this.RaiseAndSetIfChanged(ref _activeTab, value);
                this.RaisePropertyChanged(nameof(IsPositionsTabActive));
                this.RaisePropertyChanged(nameof(IsDocumentationTabActive));
                this.RaisePropertyChanged(nameof(IsDeadlinesTabActive));
            }
        }

        public ObservableCollection<OrderPositionViewModel> Positions
        {
            get => _positions;
            set => this.RaiseAndSetIfChanged(ref _positions, value);
        }

        public ObservableCollection<DocumentationViewModel> Documentations
        {
            get => _documentations;
            set => this.RaiseAndSetIfChanged(ref _documentations, value);
        }

        public ObservableCollection<DeadlineViewModel> Deadlines
        {
            get => _deadlines;
            set => this.RaiseAndSetIfChanged(ref _deadlines, value);
        }

        // Свойства для отображения вкладок
        public bool IsPositionsTabActive => ActiveTab == "positions";
        public bool IsDocumentationTabActive => ActiveTab == "documentation";
        public bool IsDeadlinesTabActive => ActiveTab == "deadlines";

        // Команды
        public ICommand NavigateToOrdersCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand AddPositionCommand { get; }
        public ICommand EditPositionCommand { get; }
        public ICommand DeletePositionCommand { get; }
        public ICommand SwitchToPositionsTabCommand { get; }
        public ICommand SwitchToDocumentationTabCommand { get; }
        public ICommand SwitchToDeadlinesTabCommand { get; }
        public ICommand DeleteOrderCommand { get; }

        private int _selectedTabIndex = 0;

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);

                // Загружаем соответствующие данные при переключении вкладок
                if (value == 1 && (Documentations == null || Documentations.Count == 0))
                {
                    System.Threading.Tasks.Task.Run(() => LoadDocumentationAsync());
                }
                else if (value == 2 && (Deadlines == null || Deadlines.Count == 0))
                {
                    System.Threading.Tasks.Task.Run(() => LoadDeadlinesAsync());
                }
            }
        }

        public OrderDetailsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int orderId)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _orderId = orderId;
            _parentWindow = null; // Будет инициализировано позже через SetParentWindow

            // Инициализация пустых коллекций для предотвращения NullReferenceException
            _positions = new ObservableCollection<OrderPositionViewModel>();
            _documentations = new ObservableCollection<DocumentationViewModel>();
            _deadlines = new ObservableCollection<DeadlineViewModel>();

            // Инициализация команд
            NavigateToOrdersCommand = ReactiveCommand.Create(NavigateToOrders);
            EditOrderCommand = ReactiveCommand.Create(EditOrderAsync);
            AddPositionCommand = ReactiveCommand.Create(AddPositionAsync);
            EditPositionCommand = ReactiveCommand.CreateFromTask<int>(EditPositionAsync);
            DeletePositionCommand = ReactiveCommand.CreateFromTask<int>(DeletePositionAsync);

            SwitchToPositionsTabCommand = ReactiveCommand.Create(() => ActiveTab = "positions");
            SwitchToDocumentationTabCommand = ReactiveCommand.Create(() => {
                ActiveTab = "documentation";
                if (_documentations.Count == 0)
                {
                    System.Threading.Tasks.Task.Run(() => LoadDocumentationAsync());
                }
            });
            SwitchToDeadlinesTabCommand = ReactiveCommand.Create(() => {
                ActiveTab = "deadlines";
                if (_deadlines.Count == 0)
                {
                    System.Threading.Tasks.Task.Run(() => LoadDeadlinesAsync());
                }
            });
            DeleteOrderCommand = ReactiveCommand.CreateFromTask(DeleteOrderAsync);
            // Асинхронная загрузка данных заказа
            System.Threading.Tasks.Task.Run(() => LoadOrderDetailsAsync(_orderId));
        }
        private async Task<bool> DeleteOrderAsync()
        {
            try
            {
                // Показываем диалог подтверждения удаления
                bool result = await MessageBoxDialog.ShowDialog(
                    _parentWindow,
                    "Подтверждение удаления",
                    "Вы действительно хотите удалить этот заказ? Это действие нельзя отменить. Все позиции заказа также будут удалены.",
                    "Удалить",
                    "Отмена");

                if (result)
                {
                    // Получаем заказ из БД
                    var order = await _dbContext.Orders.FindAsync(_orderId);
                    if (order == null)
                    {
                        Console.WriteLine($"Заказ с идентификатором {_orderId} не найден.");
                        return false;
                    }

                    // Удаляем заказ
                    _dbContext.Orders.Remove(order);
                    await _dbContext.SaveChangesAsync();

                    // Возвращаемся к списку заказов
                    _mainWindowViewModel.NavigateToOrders();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении заказа: {ex.Message}");
                return false;
            }
        }
        // Метод для установки родительского окна
        public void SetParentWindow(Window parentWindow)
        {
            _parentWindow = parentWindow;
        }

        private async System.Threading.Tasks.Task LoadOrderDetailsAsync(int orderId)
        {
            try
            {
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
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
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
                });

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
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных заказа: {ex.Message}");

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OrderNumber = "Ошибка загрузки";
                    Status = "Неизвестно";
                    Positions = new ObservableCollection<OrderPositionViewModel>();
                    IsLoading = false;
                });
            }
        }

        private async System.Threading.Tasks.Task LoadDocumentationAsync()
        {
            try
            {
                IsLoading = true;

                // Загружаем данные о документации для всех позиций заказа
                var docQuery = await _dbContext.DesignDocumentations
                    .Include(d => d.OrderPosition)
                    .Where(d => d.OrderPosition.OrderId == _orderId)
                    .ToListAsync();

                var docs = new List<DocumentationViewModel>();

                foreach (var doc in docQuery)
                {
                    var positionNumber = doc.OrderPosition?.PositionNumber ?? "Не указан";
                    var positionName = doc.OrderPosition?.ProductName ?? "Не указан";

                    // Для каждой позиции создаем записи для каждого типа документации
                    if (doc.TechnicalTaskDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "Техническое задание",
                            Date = doc.TechnicalTaskDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.TechnicalTaskDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.TechnicalTaskDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }

                    if (doc.ComponentListDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "Спецификация",
                            Date = doc.ComponentListDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.ComponentListDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.ComponentListDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }

                    if (doc.ProductCompositionDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "Состав изделия",
                            Date = doc.ProductCompositionDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.ProductCompositionDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.ProductCompositionDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }

                    if (doc.OperationManualDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "Руководство по эксплуатации",
                            Date = doc.OperationManualDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.OperationManualDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.OperationManualDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }

                    if (doc.PassportDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "Паспорт",
                            Date = doc.PassportDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.PassportDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.PassportDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }

                    if (doc.DesignDocsDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "Конструкторская документация",
                            Date = doc.DesignDocsDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.DesignDocsDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.DesignDocsDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }

                    if (doc.ElectronicDesignDocsDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "КД РЭА",
                            Date = doc.ElectronicDesignDocsDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.ElectronicDesignDocsDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.ElectronicDesignDocsDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }

                    if (doc.SoftwareDate.HasValue)
                    {
                        docs.Add(new DocumentationViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DocumentType = "Программное обеспечение",
                            Date = doc.SoftwareDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDocumentStatusByDate(doc.SoftwareDate.Value),
                            StatusColor = GetDocumentStatusColorByDate(doc.SoftwareDate.Value),
                            IsAlternate = docs.Count % 2 == 1
                        });
                    }
                }

                // Обновляем коллекцию документации в UI потоке
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Documentations = new ObservableCollection<DocumentationViewModel>(docs);
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке документации: {ex.Message}");
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task LoadDeadlinesAsync()
        {
            try
            {
                IsLoading = true;

                // Загружаем данные о сроках для всех позиций заказа
                var materialQuery = await _dbContext.MaterialsManagements
                    .Include(m => m.OrderPosition)
                    .Where(m => m.OrderPosition.OrderId == _orderId)
                    .ToListAsync();

                var productionQuery = await _dbContext.ProductionDetails
                    .Include(p => p.OrderPosition)
                    .Where(p => p.OrderPosition.OrderId == _orderId)
                    .ToListAsync();

                var deadlines = new List<DeadlineViewModel>();

                // Сроки из MaterialsManagement
                foreach (var material in materialQuery)
                {
                    var positionNumber = material.OrderPosition?.PositionNumber ?? "Не указан";
                    var positionName = material.OrderPosition?.ProductName ?? "Не указан";

                    if (material.MaterialSelectionDeadline.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Срок подбора материалов",
                            PlannedDate = material.MaterialSelectionDeadline.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(material.MaterialSelectionDeadline.Value),
                            StatusColor = GetDeadlineStatusColorByDate(material.MaterialSelectionDeadline.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (material.MaterialCompletionDeadline.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Срок комплектации",
                            PlannedDate = material.MaterialCompletionDeadline.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(material.MaterialCompletionDeadline.Value),
                            StatusColor = GetDeadlineStatusColorByDate(material.MaterialCompletionDeadline.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (material.MaterialOrderDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Заказ материалов",
                            PlannedDate = material.MaterialOrderDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(material.MaterialOrderDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(material.MaterialOrderDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (material.MaterialProvisionDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Обеспечение материалами",
                            PlannedDate = material.MaterialProvisionDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(material.MaterialProvisionDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(material.MaterialProvisionDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }
                }

                // Сроки из ProductionDetails
                foreach (var production in productionQuery)
                {
                    var positionNumber = production.OrderPosition?.PositionNumber ?? "Не указан";
                    var positionName = production.OrderPosition?.ProductName ?? "Не указан";

                    if (production.ProductionDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Изготовление",
                            PlannedDate = production.ProductionDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(production.ProductionDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(production.ProductionDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (production.DebuggingDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Отладка, прошивка",
                            PlannedDate = production.DebuggingDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(production.DebuggingDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(production.DebuggingDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (production.AcceptanceDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Приемка ОТК",
                            PlannedDate = production.AcceptanceDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(production.AcceptanceDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(production.AcceptanceDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (production.PackagingDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = positionNumber,
                            PositionName = positionName,
                            DeadlineType = "Упаковка",
                            PlannedDate = production.PackagingDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(production.PackagingDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(production.PackagingDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }
                }

                // Добавляем сроки из Order
                var order = await _dbContext.Orders.FindAsync(_orderId);
                if (order != null)
                {
                    if (order.ContractDeadline.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = "Заказ",
                            PositionName = order.Name,
                            DeadlineType = "Срок по договору",
                            PlannedDate = order.ContractDeadline.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(order.ContractDeadline.Value),
                            StatusColor = GetDeadlineStatusColorByDate(order.ContractDeadline.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (order.DeliveryDeadline.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = "Заказ",
                            PositionName = order.Name,
                            DeadlineType = "Срок поставки",
                            PlannedDate = order.DeliveryDeadline.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(order.DeliveryDeadline.Value),
                            StatusColor = GetDeadlineStatusColorByDate(order.DeliveryDeadline.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (order.ShippingDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = "Заказ",
                            PositionName = order.Name,
                            DeadlineType = "Срок отгрузки",
                            PlannedDate = order.ShippingDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(order.ShippingDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(order.ShippingDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }

                    if (order.AcceptanceDate.HasValue)
                    {
                        deadlines.Add(new DeadlineViewModel
                        {
                            PositionNumber = "Заказ",
                            PositionName = order.Name,
                            DeadlineType = "Приемка",
                            PlannedDate = order.AcceptanceDate.Value.ToString("dd.MM.yyyy"),
                            Status = GetDeadlineStatusByDate(order.AcceptanceDate.Value),
                            StatusColor = GetDeadlineStatusColorByDate(order.AcceptanceDate.Value),
                            IsAlternate = deadlines.Count % 2 == 1
                        });
                    }
                }

                // Сортируем сроки по дате (от ближайших к наиболее удаленным)
                deadlines = deadlines.OrderBy(d => DateTime.ParseExact(d.PlannedDate, "dd.MM.yyyy", null)).ToList();

                // Обновляем коллекцию сроков в UI потоке
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Deadlines = new ObservableCollection<DeadlineViewModel>(deadlines);
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке сроков: {ex.Message}");
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

        private string GetDocumentStatusByDate(DateOnly date)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (date < today)
                return "Готово";
            else if (date == today)
                return "Сегодня";
            else if (date <= today.AddDays(7))
                return "Скоро";
            else
                return "Запланировано";
        }

        private string GetDocumentStatusColorByDate(DateOnly date)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (date < today)
                return "#4CAF9D"; // Зеленый - готово
            else if (date == today)
                return "#FF7043"; // Оранжевый - сегодня
            else if (date <= today.AddDays(7))
                return "#FFB74D"; // Желтый - скоро
            else
                return "#9575CD"; // Фиолетовый - запланировано
        }

        private string GetDeadlineStatusByDate(DateOnly date)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (date < today)
                return "Просрочено";
            else if (date == today)
                return "Сегодня";
            else if (date <= today.AddDays(7))
                return "Скоро";
            else
                return "Запланировано";
        }

        private string GetDeadlineStatusColorByDate(DateOnly date)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (date < today)
                return "#FF7043"; // Оранжевый - просрочено
            else if (date == today)
                return "#FFB74D"; // Желтый - сегодня
            else if (date <= today.AddDays(7))
                return "#00ACC1"; // Голубой - скоро
            else
                return "#9575CD"; // Фиолетовый - запланировано
        }

        private void NavigateToOrders()
        {
            _mainWindowViewModel.NavigateToOrders();
        }

        private async Task<bool> EditOrderAsync()
        {
            try
            {
                return await Views.Dialogs.OrderEditDialog.ShowDialogAsync(_parentWindow, _dbContext, _orderId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии диалога редактирования заказа: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> AddPositionAsync()
        {
            try
            {
                var result = await Views.Dialogs.OrderPositionDialog.ShowDialogAsync(_parentWindow, _dbContext, _orderId);

                // Если позиция была добавлена, обновляем список позиций
                if (result)
                {
                    await LoadOrderDetailsAsync(_orderId);
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии диалога добавления позиции: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> EditPositionAsync(int positionId)
        {
            try
            {
                var result = await Views.Dialogs.OrderPositionDialog.ShowEditDialogAsync(_parentWindow, _dbContext, positionId);

                // Если позиция была отредактирована, обновляем список позиций
                if (result)
                {
                    await LoadOrderDetailsAsync(_orderId);
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии диалога редактирования позиции: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> DeletePositionAsync(int positionId)
        {
            try
            {
                // Показываем диалог подтверждения удаления
                bool result = await MessageBoxDialog.ShowDialog(
                    _parentWindow,
                    "Подтверждение удаления",
                    "Вы действительно хотите удалить эту позицию? Это действие нельзя отменить.",
                    "Удалить",
                    "Отмена");

                if (result)
                {
                    // Получаем позицию из БД
                    var position = await _dbContext.OrderPositions.FindAsync(positionId);
                    if (position == null)
                    {
                        Console.WriteLine($"Позиция с идентификатором {positionId} не найдена.");
                        return false;
                    }
                    // Удаляем позицию
                    _dbContext.OrderPositions.Remove(position);
                    await _dbContext.SaveChangesAsync();
                    // Обновляем список позиций
                    await LoadOrderDetailsAsync(_orderId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении позиции: {ex.Message}");
                return false;
            }
        }


        // Вспомогательные классы для отображения данных в таблицах
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

        public class DocumentationViewModel
        {
            public string PositionNumber { get; set; }
            public string PositionName { get; set; }
            public string DocumentType { get; set; }
            public string Date { get; set; }
            public string Status { get; set; }
            public string StatusColor { get; set; }
            public bool IsAlternate { get; set; }
        }

        public class DeadlineViewModel
        {
            public string PositionNumber { get; set; }
            public string PositionName { get; set; }
            public string DeadlineType { get; set; }
            public string PlannedDate { get; set; }
            public string Status { get; set; }
            public string StatusColor { get; set; }
            public bool IsAlternate { get; set; }
        }
    }
}