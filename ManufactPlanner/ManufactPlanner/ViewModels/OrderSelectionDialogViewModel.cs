// ViewModels/Dialogs/OrderSelectionDialogViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using Microsoft.EntityFrameworkCore;
using Avalonia.Controls;

namespace ManufactPlanner.ViewModels.Dialogs
{
    public class OrderSelectionDialogViewModel : ViewModelBase
    {
        private readonly PostgresContext _dbContext;
        private ObservableCollection<OrderListItem> _orders;
        private ObservableCollection<OrderListItem> _filteredOrders;
        private OrderListItem _selectedOrder;
        private string _searchText = string.Empty;
        private bool _dialogResult = false;

        // Ссылка на окно диалога для закрытия
        private Window _dialogWindow;

        public ObservableCollection<OrderListItem> Orders
        {
            get => _orders;
            set => this.RaiseAndSetIfChanged(ref _orders, value);
        }

        public ObservableCollection<OrderListItem> FilteredOrders
        {
            get => _filteredOrders;
            set => this.RaiseAndSetIfChanged(ref _filteredOrders, value);
        }

        public OrderListItem SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedOrder, value);
                this.RaisePropertyChanged(nameof(IsOrderSelected));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                FilterOrders();
            }
        }

        public bool IsOrderSelected => SelectedOrder != null;

        public bool DialogResult
        {
            get => _dialogResult;
            set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
        }

        // Добавляем возможность установить окно диалога
        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public OrderSelectionDialogViewModel(PostgresContext dbContext)
        {
            _dbContext = dbContext;
            ConfirmCommand = ReactiveCommand.Create(ConfirmSelection);
            CancelCommand = ReactiveCommand.Create(CancelSelection);

            LoadOrders();
        }

        private async void LoadOrders()
        {
            try
            {
                var ordersList = await _dbContext.Orders
                    .Select(o => new OrderListItem
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Name = o.Name,
                        Customer = o.Customer
                    })
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();

                Orders = new ObservableCollection<OrderListItem>(ordersList);
                FilteredOrders = new ObservableCollection<OrderListItem>(ordersList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке списка заказов: {ex.Message}");

                // Заглушка для дизайнера или при ошибке
                if (Orders == null || Orders.Count == 0)
                {
                    Orders = new ObservableCollection<OrderListItem>
                    {
                        new OrderListItem { Id = 1, OrderNumber = "ОП-113/24", Name = "Керченский морской технический колледж. 2024" },
                        new OrderListItem { Id = 2, OrderNumber = "ОП-136/24", Name = "Колледж современных технологий. Учебный лабораторный стенд" },
                        new OrderListItem { Id = 3, OrderNumber = "ОП-168/24", Name = "Губкинский колледж. Электрооборудование" }
                    };
                    FilteredOrders = new ObservableCollection<OrderListItem>(Orders);
                }
            }
        }

        private void FilterOrders()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredOrders = new ObservableCollection<OrderListItem>(Orders);
                return;
            }

            var searchText = SearchText.ToLower();
            var filtered = Orders.Where(o =>
                o.OrderNumber.ToLower().Contains(searchText) ||
                o.Name.ToLower().Contains(searchText) ||
                (o.Customer?.ToLower().Contains(searchText) ?? false)).ToList();

            FilteredOrders = new ObservableCollection<OrderListItem>(filtered);
        }

        private void ConfirmSelection()
        {
            DialogResult = true;
            CloseWindow();
        }

        private void CancelSelection()
        {
            DialogResult = false;
            CloseWindow();
        }

        private void CloseWindow()
        {
            _dialogWindow?.Close();
        }
    }

    public class OrderListItem
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Name { get; set; }
        public string Customer { get; set; }

        public override string ToString()
        {
            return $"{OrderNumber} - {Name}";
        }
    }
}