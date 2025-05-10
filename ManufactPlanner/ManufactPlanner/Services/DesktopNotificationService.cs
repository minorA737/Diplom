// Services/DesktopNotificationService.cs
using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ManufactPlanner.Services
{
    public class DesktopNotificationService
    {
        private static NotifyIcon _notifyIcon;
        private static readonly object _lockObject = new object();

        public static void Initialize()
        {
            if (_notifyIcon == null)
            {
                lock (_lockObject)
                {
                    if (_notifyIcon == null)
                    {
                        _notifyIcon = new NotifyIcon
                        {
                            Icon = GetApplicationIcon(),
                            Visible = true,
                            Text = "ManufactPlanner"
                        };
                    }
                }
            }
        }

        public static void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (_notifyIcon == null)
            {
                Initialize();
            }

            var icon = type switch
            {
                NotificationType.Info => ToolTipIcon.Info,
                NotificationType.Warning => ToolTipIcon.Warning,
                NotificationType.Error => ToolTipIcon.Error,
                _ => ToolTipIcon.Info
            };

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(5000); // Показывать 5 секунд
        }

        private static Icon GetApplicationIcon()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly();
                var iconPath = Path.Combine(Path.GetDirectoryName(assembly.Location), "Assets", "logo.jpg");

                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                // Если файл не найден, используем системную иконку
                return SystemIcons.Application;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        public static void Cleanup()
        {
            _notifyIcon?.Dispose();
            _notifyIcon = null;
        }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }
}