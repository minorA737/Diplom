// Services/LocalizationService.cs
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace ManufactPlanner.Services
{
    public class LocalizationService : ReactiveObject
    {
        private static LocalizationService _instance;
        private Dictionary<string, Dictionary<string, string>> _translations;
        private string _currentLanguage = "ru";

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentLanguage, value);
                NotifyTranslationsChanged();
            }
        }

        public event EventHandler TranslationsChanged;

        public LocalizationService()
        {
            InitializeTranslations();
        }

        private void InitializeTranslations()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["ru"] = new Dictionary<string, string>
                {
                    // Профиль пользователя
                    ["ProfileSettings"] = "Профиль пользователя",
                    ["Username"] = "Имя пользователя:",
                    ["FirstName"] = "Имя:",
                    ["LastName"] = "Фамилия:",
                    ["Email"] = "Email:",
                    ["SaveChanges"] = "Сохранить изменения",

                    // Изменение пароля
                    ["PasswordSettings"] = "Изменение пароля",
                    ["CurrentPassword"] = "Текущий пароль:",
                    ["NewPassword"] = "Новый пароль:",
                    ["ConfirmPassword"] = "Подтверждение:",
                    ["ChangePassword"] = "Изменить пароль",

                    // Настройки уведомлений
                    ["NotificationSettings"] = "Настройки уведомлений",
                    ["NotifyNewTasks"] = "Уведомления о новых задачах",
                    ["NotifyStatusChanges"] = "Уведомления об изменении статуса задач",
                    ["NotifyComments"] = "Уведомления о комментариях",
                    ["NotifyDeadlines"] = "Уведомления о приближающихся дедлайнах",
                    ["NotifyEmail"] = "Уведомления по электронной почте",
                    ["NotifyDesktop"] = "Уведомления на рабочем столе",
                    ["SaveNotificationSettings"] = "Сохранить настройки",

                    // Настройки интерфейса
                    ["InterfaceSettings"] = "Настройки интерфейса",
                    ["Language"] = "Язык интерфейса:",
                    ["Theme"] = "Тема:",
                    ["LightTheme"] = "Светлая",
                    ["DarkTheme"] = "Тёмная",
                    ["Apply"] = "Применить",

                    // О программе
                    ["AboutApp"] = "О программе",
                    ["AppName"] = "ManufactPlanner: Система автоматизации планирования разработки изделий",
                    ["Version"] = "Версия: 1.0.0 (сборка от 26.04.2025)",
                    ["Copyright"] = "© 2025 Все права защищены",
                    ["DevelopedAs"] = "Разработано в рамках дипломного проекта",
                    ["Technologies"] = "Используемые технологии: Avalonia UI, .NET 8, PostgreSQL",

                    // Сообщения
                    ["PasswordMismatch"] = "Новый пароль и подтверждение не совпадают",
                    ["PasswordChangedSuccess"] = "Пароль успешно изменен",
                    ["PasswordChangedError"] = "Ошибка при изменении пароля",
                    ["ProfileSavedSuccess"] = "Профиль успешно сохранен",
                    ["ProfileSavedError"] = "Ошибка при сохранении профиля"
                },
                ["en"] = new Dictionary<string, string>
                {
                    // User Profile
                    ["ProfileSettings"] = "User Profile",
                    ["Username"] = "Username:",
                    ["FirstName"] = "First Name:",
                    ["LastName"] = "Last Name:",
                    ["Email"] = "Email:",
                    ["SaveChanges"] = "Save Changes",

                    // Password Change
                    ["PasswordSettings"] = "Change Password",
                    ["CurrentPassword"] = "Current Password:",
                    ["NewPassword"] = "New Password:",
                    ["ConfirmPassword"] = "Confirm Password:",
                    ["ChangePassword"] = "Change Password",

                    // Notification Settings
                    ["NotificationSettings"] = "Notification Settings",
                    ["NotifyNewTasks"] = "Notify about new tasks",
                    ["NotifyStatusChanges"] = "Notify about task status changes",
                    ["NotifyComments"] = "Notify about comments",
                    ["NotifyDeadlines"] = "Notify about approaching deadlines",
                    ["NotifyEmail"] = "Email notifications",
                    ["NotifyDesktop"] = "Desktop notifications",
                    ["SaveNotificationSettings"] = "Save Settings",

                    // Interface Settings
                    ["InterfaceSettings"] = "Interface Settings",
                    ["Language"] = "Interface Language:",
                    ["Theme"] = "Theme:",
                    ["LightTheme"] = "Light",
                    ["DarkTheme"] = "Dark",
                    ["Apply"] = "Apply",

                    // About App
                    ["AboutApp"] = "About",
                    ["AppName"] = "ManufactPlanner: Product Development Planning Automation System",
                    ["Version"] = "Version: 1.0.0 (build 26.04.2025)",
                    ["Copyright"] = "© 2025 All rights reserved",
                    ["DevelopedAs"] = "Developed as a diploma project",
                    ["Technologies"] = "Technologies used: Avalonia UI, .NET 8, PostgreSQL",

                    // Messages
                    ["PasswordMismatch"] = "New password and confirmation do not match",
                    ["PasswordChangedSuccess"] = "Password successfully changed",
                    ["PasswordChangedError"] = "Error changing password",
                    ["ProfileSavedSuccess"] = "Profile successfully saved",
                    ["ProfileSavedError"] = "Error saving profile"
                }
            };
        }

        public string GetString(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out var languageDict) &&
                languageDict.TryGetValue(key, out var translation))
            {
                return translation;
            }

            // Возвращаем ключ, если перевод не найден
            return key;
        }

        private void NotifyTranslationsChanged()
        {
            TranslationsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetLanguage(string language)
        {
            if (_translations.ContainsKey(language))
            {
                CurrentLanguage = language;

                // Устанавливаем культуру для приложения
                var culture = language == "ru" ? new CultureInfo("ru-RU") : new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
        }
    }
}