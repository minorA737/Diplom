// Views/Dialogs/OrderPositionSelectionDialog.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.ViewModels.Dialogs;
using ManufactPlanner.Models;
using System.Collections.Generic;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class OrderPositionSelectionDialog : Window
    {
        public OrderPositionSelectionDialog()
        {
            InitializeComponent();
        }

        public OrderPositionSelectionDialog(List<OrderPosition> orderPositions)
        {
            InitializeComponent();
            var viewModel = new OrderPositionSelectionDialogViewModel(orderPositions);
            DataContext = viewModel;

            // Устанавливаем ссылку на окно в ViewModel
            viewModel.SetDialogWindow(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}