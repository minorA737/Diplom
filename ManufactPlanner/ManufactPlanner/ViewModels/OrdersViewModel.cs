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
    public class OrdersViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private ObservableCollection<OrderItemViewModel> _orders;

        public ObservableCollection<OrderItemViewModel> Orders
        {
            get => _orders;
            set => this.RaiseAndSetIfChanged(ref _orders, value);
        }

        public ICommand CreateOrderCommand { get; }
        public ICommand ShowOrderDetailsCommand { get; }

        public OrdersViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            CreateOrderCommand = ReactiveCommand.Create(CreateOrder);
            ShowOrderDetailsCommand = ReactiveCommand.Create<int>(ShowOrderDetails);

            LoadOrders();
        }

        private void LoadOrders()
        {
            // В реальном приложении здесь будет загрузка данных из БД
            // Для примера используем тестовые данные
            Orders = new ObservableCollection<OrderItemViewModel>
            {
                new OrderItemViewModel { Id = 1, OrderNumber = "ОП-113/24", Name = "Институт развития проф. образования", Customer = "ИРПО", Deadline = "10.10.2024", PositionsCount = "1", Status = "Активен", IsAlternate = false },
                new OrderItemViewModel { Id = 2, OrderNumber = "ОП-136/24", Name = "Колледж современных технологий", Customer = "КСТ", Deadline = "12.02.2025", PositionsCount = "12", Status = "Активен", IsAlternate = true, IsDateCritical = true },
                new OrderItemViewModel { Id = 3, OrderNumber = "ОП-141/24", Name = "Губкинский горно-политехнический колледж", Customer = "ГГПК", Deadline = "14.10.2024", PositionsCount = "1", Status = "Активен", IsAlternate = false },
                new OrderItemViewModel { Id = 4, OrderNumber = "ОП-145/24", Name = "ГГПК. Подземная разработка МПИ", Customer = "ГГПК", Deadline = "14.10.2024", PositionsCount = "2", Status = "Активен", IsAlternate = true },
                new OrderItemViewModel { Id = 5, OrderNumber = "ОП-168/24", Name = "ГГПК. Электрооборудование", Customer = "ГГПК", Deadline = "17.02.2024", PositionsCount = "7", Status = "Активен", IsAlternate = false, IsDateCritical = true },
                new OrderItemViewModel { Id = 6, OrderNumber = "ОП-169/24", Name = "АНО ПО \"МИКА\". Тренажер АРМ", Customer = "АНО ПО \"МИКА\"", Deadline = "01.05.2025", PositionsCount = "1", Status = "Активен", IsAlternate = true }
            };
        }

        private void CreateOrder()
        {
            // В реальном приложении здесь будет логика создания нового заказа
            // Например, открытие диалогового окна и т.д.
        }

        private void ShowOrderDetails(int orderId)
        {
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
        public bool IsAlternate { get; set; }
        public bool IsDateCritical { get; set; }
    }
}
