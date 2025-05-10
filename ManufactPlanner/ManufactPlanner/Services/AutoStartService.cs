// Services/AutoStartService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ManufactPlanner.Services
{
    public static class AutoStartService
    {
        private const string AppName = "ManufactPlanner";

        public static bool IsAutoStartEnabled()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                    return key?.GetValue(AppName) != null;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    string autoStartPath = GetLinuxAutoStartPath();
                    return File.Exists(autoStartPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    string autoStartPlist = GetMacOSAutoStartPath();
                    return File.Exists(autoStartPlist);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке автозапуска: {ex.Message}");
            }

            return false;
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SetWindowsAutoStart(enable);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    SetLinuxAutoStart(enable);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    SetMacOSAutoStart(enable);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при настройке автозапуска: {ex.Message}");
            }
        }

        private static void SetWindowsAutoStart(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (key == null)
                return;

            if (enable)
            {
                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                key.SetValue(AppName, appPath);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }

        private static void SetLinuxAutoStart(bool enable)
        {
            string autoStartPath = GetLinuxAutoStartPath();
            string autoStartDir = Path.GetDirectoryName(autoStartPath);

            if (!Directory.Exists(autoStartDir))
                Directory.CreateDirectory(autoStartDir);

            if (enable)
            {
                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                string desktopEntry = $"""
                [Desktop Entry]
                Type=Application
                Exec={appPath}
                Hidden=false
                NoDisplay=false
                X-GNOME-Autostart-enabled=true
                Name[en_US]={AppName}
                Name={AppName}
                Comment[en_US]=Start {AppName} when user logs in
                Comment=Start {AppName} when user logs in
                """;

                File.WriteAllText(autoStartPath, desktopEntry);
            }
            else if (File.Exists(autoStartPath))
            {
                File.Delete(autoStartPath);
            }
        }

        private static void SetMacOSAutoStart(bool enable)
        {
            string plistPath = GetMacOSAutoStartPath();
            string launchAgentsDir = Path.GetDirectoryName(plistPath);

            if (!Directory.Exists(launchAgentsDir))
                Directory.CreateDirectory(launchAgentsDir);

            if (enable)
            {
                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                string plistContent = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                    <key>Label</key>
                    <string>com.{AppName.ToLower()}.launcher</string>
                    <key>ProgramArguments</key>
                    <array>
                        <string>{appPath}</string>
                    </array>
                    <key>RunAtLoad</key>
                    <true/>
                </dict>
                </plist>
                """;

                File.WriteAllText(plistPath, plistContent);

                // Загружаем службу
                using var process = new Process();
                process.StartInfo.FileName = "launchctl";
                process.StartInfo.Arguments = $"load {plistPath}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
            }
            else if (File.Exists(plistPath))
            {
                // Выгружаем службу перед удалением
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "launchctl";
                    process.StartInfo.Arguments = $"unload {plistPath}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                }

                File.Delete(plistPath);
            }
        }

        private static string GetLinuxAutoStartPath()
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".config", "autostart", $"{AppName.ToLower()}.desktop");
        }

        private static string GetMacOSAutoStartPath()
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, "Library", "LaunchAgents", $"com.{AppName.ToLower()}.launcher.plist");
        }
    }
}