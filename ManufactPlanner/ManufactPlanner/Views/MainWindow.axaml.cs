using Avalonia.Controls;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Views.Dialogs;
using System.ComponentModel;

namespace ManufactPlanner.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppWindows.MainWindow = this;

            // ������������ ������� �������� ����
            Closing += OnClosing;
        }

        private async void OnClosing(object sender, CancelEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel == null)
                return;

            e.Cancel = true; // �������� �������� �� ���������

            // ���������� ������ ������
            var result = await MessageBoxDialog.ShowDialog(
                this,
                "�������� ����������",
                "�� ������ ������� ���������� ��������� ��� �������� ��� �������� � ������� ������?",
                "������� ���������",
                "� ������� ������"
            );

            if (result)
            {
                // ������������ ������ ������ ��������
                viewModel.ForceExit();
            }
            else
            {
                // ������������ ������ ������ � ����
                viewModel.HideToTray();
            }
        }
    }
}