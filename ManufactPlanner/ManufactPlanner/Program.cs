using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Diagnostics;

namespace ManufactPlanner
{
    internal sealed class Program
    {
        private static Mutex _mutex;
        private static bool _createdNew;
        private const string PIPE_NAME = "ManufactPlannerPipe";
        private const string MUTEX_NAME = "ManufactPlannerSingleInstance";
        private static NamedPipeServerStream _pipeServer;

        [STAThread]
        public static void Main(string[] args)
        {
            // Проверяем, что экземпляр приложения уже не запущен
            _mutex = new Mutex(true, MUTEX_NAME, out _createdNew);

            if (!_createdNew)
            {
                // Приложение уже запущено - отправляем сигнал для его активации
                Debug.WriteLine("Приложение уже запущено. Пытаемся активировать существующий экземпляр...");
                SendActivationSignal();
                return;
            }

            try
            {
                Debug.WriteLine("Запускаем новый экземпляр приложения...");

                // Запускаем сервер для прослушивания сигналов активации
                StartPipeServer();

                // Запускаем приложение
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                // Очищаем ресурсы
                ClosePipeServer();
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();

        private static void SendActivationSignal()
        {
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out))
                {
                    pipeClient.Connect(3000); // Ждем максимум 3 секунды

                    byte[] message = Encoding.UTF8.GetBytes("ACTIVATE");
                    pipeClient.Write(message, 0, message.Length);
                    pipeClient.Flush();

                    Debug.WriteLine("Сигнал активации отправлен.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не удалось отправить сигнал активации: {ex.Message}");
            }
        }

        private static void StartPipeServer()
        {
            Debug.WriteLine("Запускаем pipe сервер...");

            // Запускаем сервер в отдельном потоке
            var serverThread = new Thread(() =>
            {
                try
                {
                    // Закрываем старый сервер, если он есть
                    _pipeServer?.Dispose();

                    while (true)
                    {
                        _pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.In, 1);
                        Debug.WriteLine("Ожидание подключения к pipe...");

                        _pipeServer.WaitForConnection();
                        Debug.WriteLine("Клиент подключен к pipe.");

                        // Читаем сообщение
                        var buffer = new byte[1024];
                        int bytesRead = _pipeServer.Read(buffer, 0, buffer.Length);
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        Debug.WriteLine($"Получено сообщение: {message}");

                        if (message == "ACTIVATE")
                        {
                            Debug.WriteLine("Активируем приложение...");

                            // Активируем приложение в UI потоке
                            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ActivateApp();
                            });
                        }

                        _pipeServer.Disconnect();
                        _pipeServer.Dispose();
                    }
                }
                catch (ObjectDisposedException)
                {
                    Debug.WriteLine("Pipe server был корректно закрыт.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка в pipe сервере: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "PipeServerThread"
            };

            serverThread.Start();
        }

        private static void ActivateApp()
        {
            Debug.WriteLine("Попытка активации приложения...");

            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;

                    if (mainWindow?.DataContext is ManufactPlanner.ViewModels.MainWindowViewModel mainViewModel)
                    {
                        Debug.WriteLine($"Состояние окна - Visible: {mainWindow.IsVisible}, WindowState: {mainWindow.WindowState}");

                        // Если приложение в трее или скрыто, восстанавливаем его
                        if (!mainWindow.IsVisible || mainWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                        {
                            Debug.WriteLine("Восстанавливаем приложение из трея/минимизированного состояния...");
                            mainViewModel.RestoreFromTray();
                        }
                        else
                        {
                            Debug.WriteLine("Активируем видимое окно...");
                            // Если окно видимо, просто активируем его
                            mainWindow.Activate();
                            mainWindow.Topmost = true;
                            // Немного задержки чтобы окно точно активировалось
                            System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
                            {
                                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    mainWindow.Topmost = false;
                                });
                            });
                        }

                        Debug.WriteLine("Активация приложения завершена.");
                    }
                    else
                    {
                        Debug.WriteLine("Не удалось получить MainWindowViewModel");
                    }
                }
                else
                {
                    Debug.WriteLine("Не удалось получить ApplicationLifetime");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при активации приложения: {ex.Message}");
            }
        }

        private static void ClosePipeServer()
        {
            try
            {
                _pipeServer?.Dispose();
                Debug.WriteLine("Pipe server закрыт.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при закрытии pipe server: {ex.Message}");
            }
        }
    }
}