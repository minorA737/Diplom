using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ManufactPlanner.Views;
using System;
using System.Threading.Tasks;

namespace ManufactPlanner.Services
{
    public static class NotificationWindowService
    {
        public static void ShowNotification(string title, string message)
        {
            if (Avalonia.Application.Current == null)
                return;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var window = new Window
                {
                    Title = "Уведомление",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    CanResize = false,
                    ShowInTaskbar = false,
                    Topmost = true,
                    SystemDecorations = SystemDecorations.None,
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(8)
                };

                var panel = new StackPanel
                {
                    Margin = new Thickness(15),
                    Spacing = 10
                };

                var titleBlock = new TextBlock
                {
                    Text = title,
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                var messageBlock = new TextBlock
                {
                    Text = message,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Colors.DarkGray)
                };

                panel.Children.Add(titleBlock);
                panel.Children.Add(messageBlock);

                window.Content = panel;

                // Позиционируем окно в правом нижнем углу экрана
                var screen = window.Screens.Primary;
                if (screen != null)
                {
                    var workingArea = screen.WorkingArea;
                    window.Position = new PixelPoint(
                        (int)(workingArea.Width - window.Width - 20),
                        (int)(workingArea.Height - window.Height - 20)
                    );
                }

                window.Show();

                // Автоматически закрываем окно через 5 секунд
                Task.Delay(5000).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        window.Close();
                    });
                });

                // Добавляем обработчик клика для закрытия
                window.PointerPressed += (s, e) => window.Close();
            });
        }
    }
}