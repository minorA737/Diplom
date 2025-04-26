using ManufactPlanner.Models;
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

        private string _orderNumber = "ОП-168/24";
        private string _orderName = "Губкинский горно-политехнический колледж. Эксплуатация и обслуживание электрического и электромеханического оборудования. 2024";
        private string _customer = "Губкинский горно-политехнический колледж";
        private string _contractDeadline = "20.12.2024";
        private string _deliveryDeadline = "17.02.2024";
        private bool _isDeliveryDateCritical = true;
        private string _contractQuantity = "7 шт.";
        private string _totalPrice = "6 667 599,18 руб.";
        private string _status = "Активен";

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

            LoadOrderDetails(orderId);
        }

        public OrderDetailsViewModel()
        {
            // Конструктор для дизайнера
            NavigateToOrdersCommand = ReactiveCommand.Create(NavigateToOrders);
            EditOrderCommand = ReactiveCommand.Create(EditOrder);
            AddPositionCommand = ReactiveCommand.Create(AddPosition);
            EditPositionCommand = ReactiveCommand.Create<int>(EditPosition);
            CreateTaskCommand = ReactiveCommand.Create<int>(CreateTask);

            LoadTestData();
        }

        private void LoadOrderDetails(int orderId)
        {
            // В реальном приложении здесь будет загрузка данных о заказе из БД
            // Для примера используем тестовые данные
            LoadTestData();
        }

        private void LoadTestData()
        {
            // Пример данных позиций заказа
            Positions = new ObservableCollection<OrderPositionViewModel>
            {
                new OrderPositionViewModel { Id = 1, PositionNumber = "1", ProductName = "Комплект лабораторного оборудования", Quantity = "1", Price = "10 453 200,03 руб.", DevelopmentType = "Разработка", Status = "В процессе", StatusColor = "#00ACC1", IsAlternate = false },
                new OrderPositionViewModel { Id = 2, PositionNumber = "1.2", ProductName = "Стенд учебный \"Монтаж и подключение контрольно-измерительных приборов гидравлических и механических величин\"", Quantity = "1", Price = "1 341 795,40 руб.", DevelopmentType = "Разработка", Status = "В процессе", StatusColor = "#00ACC1", IsAlternate = true },
                new OrderPositionViewModel { Id = 3, PositionNumber = "1.6", ProductName = "Стенд \"Основы электромеханики и электроники\"", Quantity = "1", Price = "926 402,55 руб.", DevelopmentType = "Покупное", Status = "В очереди", StatusColor = "#9575CD", IsAlternate = false },
                new OrderPositionViewModel { Id = 4, PositionNumber = "1.7", ProductName = "Стенд \"Силовая электроника и электропривод\"", Quantity = "1", Price = "787 444,35 руб.", DevelopmentType = "Разработка", Status = "В очереди", StatusColor = "#9575CD", IsAlternate = true },
                new OrderPositionViewModel { Id = 5, PositionNumber = "1.8", ProductName = "Стенд \"Электротехника, электроника, электрические машины и электропривод\"", Quantity = "1", Price = "1 204 318,96 руб.", DevelopmentType = "Разработка", Status = "В процессе", StatusColor = "#00ACC1", IsAlternate = false },
                new OrderPositionViewModel { Id = 6, PositionNumber = "1.9", ProductName = "Стенд \"Микропроцессорные системы управления электроприводов\"", Quantity = "1", Price = "694 801,91 руб.", DevelopmentType = "Покупное", Status = "В очереди", StatusColor = "#9575CD", IsAlternate = true },
                new OrderPositionViewModel { Id = 7, PositionNumber = "1.13", ProductName = "Стенд \"Основы релейной защиты и автоматики\"", Quantity = "1", Price = "602 159,48 руб.", DevelopmentType = "Покупное", Status = "В очереди", StatusColor = "#9575CD", IsAlternate = false },
                new OrderPositionViewModel { Id = 8, PositionNumber = "1.14", ProductName = "Стенд \"Электрические станции и подстанции\"", Quantity = "1", Price = "1 111 676,53 руб.", DevelopmentType = "Разработка", Status = "Ждем производство", StatusColor = "#FFB74D", IsAlternate = true }
            };
        }

        private void NavigateToOrders()
        {
            _mainWindowViewModel.NavigateToOrders();
        }

        private void EditOrder()
        {
            // Логика редактирования заказа
        }

        private void AddPosition()
        {
            // Логика добавления новой позиции в заказ
        }

        private void EditPosition(int positionId)
        {
            // Логика редактирования позиции заказа
        }

        private void CreateTask(int positionId)
        {
            // Логика создания задачи для позиции заказа
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