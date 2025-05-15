using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Services;
using ManufactPlanner.ViewModels;
using System;

namespace ManufactPlanner.Views
{
    public partial class DesktopNotificationDialog : Window
    {
        private NotificationDialogViewModel _viewModel;

        public DesktopNotificationDialog()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public DesktopNotificationDialog(NotificationViewModel notification, MainWindowViewModel mainWindowViewModel, NotificationService notificationService) : this()
        {
            _viewModel = new NotificationDialogViewModel(notification, mainWindowViewModel, notificationService, this);
            DataContext = _viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void OnWindowOpened(object sender, EventArgs e)
        {
            var screen = Screens.Primary;  // Получаем основной экран
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                int margin = 100;  // Отступ от краёв

                this.Position = new PixelPoint(
                    (int)(workingArea.Right - this.Width - margin),   // X: правый край - ширина окна - отступ
                    (int)(workingArea.Bottom - this.Height - margin)   // Y: нижний край - высота окна - отступ
                );
            }
        }
    }
}