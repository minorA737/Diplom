// ViewModels/Dialogs/OrderPositionSelectionDialogViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using Avalonia.Controls;

namespace ManufactPlanner.ViewModels.Dialogs
{
    public class OrderPositionSelectionDialogViewModel : ViewModelBase
    {
        private ObservableCollection<OrderPosition> _orderPositions;
        private ObservableCollection<OrderPosition> _filteredOrderPositions;
        private OrderPosition _selectedOrderPosition;
        private string _searchText = string.Empty;
        private bool _dialogResult = false;
        private string _dialogTitle = "Выберите позицию заказа для документа";
        private Window _dialogWindow;

        public ObservableCollection<OrderPosition> OrderPositions
        {
            get => _orderPositions;
            set => this.RaiseAndSetIfChanged(ref _orderPositions, value);
        }

        public ObservableCollection<OrderPosition> FilteredOrderPositions
        {
            get => _filteredOrderPositions;
            set => this.RaiseAndSetIfChanged(ref _filteredOrderPositions, value);
        }

        public OrderPosition SelectedOrderPosition
        {
            get => _selectedOrderPosition;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedOrderPosition, value);
                this.RaisePropertyChanged(nameof(IsPositionSelected));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                FilterPositions();
            }
        }

        public string DialogTitle
        {
            get => _dialogTitle;
            set => this.RaiseAndSetIfChanged(ref _dialogTitle, value);
        }

        public bool IsPositionSelected => SelectedOrderPosition != null;

        public bool DialogResult
        {
            get => _dialogResult;
            set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
        }

        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        public ICommand ConfirmCommand { get; }
        public ICommand SkipCommand { get; }

        public OrderPositionSelectionDialogViewModel(List<OrderPosition> orderPositions)
        {
            ConfirmCommand = ReactiveCommand.Create(ConfirmSelection);
            SkipCommand = ReactiveCommand.Create(SkipSelection);

            if (orderPositions != null && orderPositions.Any())
            {
                var orderNumber = orderPositions.FirstOrDefault()?.Order?.OrderNumber ?? "";
                DialogTitle = $"Выберите позицию заказа {orderNumber}";

                OrderPositions = new ObservableCollection<OrderPosition>(orderPositions);
                FilteredOrderPositions = new ObservableCollection<OrderPosition>(orderPositions);
            }
            else
            {
                DialogTitle = "Нет доступных позиций для этого заказа";
                OrderPositions = new ObservableCollection<OrderPosition>();
                FilteredOrderPositions = new ObservableCollection<OrderPosition>();
            }
        }

        private void FilterPositions()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredOrderPositions = new ObservableCollection<OrderPosition>(OrderPositions);
                return;
            }

            var searchText = SearchText.ToLower();
            var filtered = OrderPositions.Where(p =>
                p.PositionNumber.ToLower().Contains(searchText) ||
                p.ProductName.ToLower().Contains(searchText)).ToList();

            FilteredOrderPositions = new ObservableCollection<OrderPosition>(filtered);
        }

        private void ConfirmSelection()
        {
            DialogResult = true;
            CloseWindow();
        }

        private void SkipSelection()
        {
            // Пользователь решил не выбирать позицию
            SelectedOrderPosition = null;
            DialogResult = true;
            CloseWindow();
        }

        private void CloseWindow()
        {
            _dialogWindow?.Close();
        }
    }
}