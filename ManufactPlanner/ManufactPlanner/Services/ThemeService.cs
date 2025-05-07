// Services/ThemeService.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;

namespace ManufactPlanner.Services
{
    public class ThemeService : ReactiveObject
    {
        private static ThemeService _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        private bool _isLightTheme = true;
        private readonly BehaviorSubject<bool> _themeChanged = new BehaviorSubject<bool>(true);

        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                this.RaiseAndSetIfChanged(ref _isLightTheme, value);
                Application.Current.RequestedThemeVariant = value ? ThemeVariant.Light : ThemeVariant.Dark;
                _themeChanged.OnNext(value);
                ApplyTheme();
            }
        }

        public IObservable<bool> ThemeChanged => _themeChanged;
        private ThemeService()
        {
            // Инициализация с системной темой или сохраненными настройками
            // Можно добавить загрузку сохраненной темы из настроек
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            if (Application.Current == null) return;

            // Получаем ресурсы приложения
            var resources = Application.Current.Resources;

            if (IsLightTheme)
            {
                // Светлая тема
                resources["BackgroundColor"] = Avalonia.Media.Color.Parse("#F8F9FA");
                resources["SurfaceColor"] = Avalonia.Media.Color.Parse("#FFFFFF");
                resources["TextPrimaryColor"] = Avalonia.Media.Color.Parse("#333333");
                resources["TextSecondaryColor"] = Avalonia.Media.Color.Parse("#666666");
                resources["BorderColor"] = Avalonia.Media.Color.Parse("#E0E0E0");
                resources["ShadowColor"] = Avalonia.Media.Color.Parse("#10000000");

                resources["BackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F8F9FA"));
                resources["SurfaceBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
                resources["TextPrimaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#333333"));
                resources["TextSecondaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#666666"));
                resources["BorderBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E0E0E0"));
                resources["ShadowBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10000000"));
            }
            else
            {
                // Темная тема
                resources["BackgroundColor"] = Avalonia.Media.Color.Parse("#121212");
                resources["SurfaceColor"] = Avalonia.Media.Color.Parse("#222222");
                resources["TextPrimaryColor"] = Avalonia.Media.Color.Parse("#FFFFFF");
                resources["TextSecondaryColor"] = Avalonia.Media.Color.Parse("#AAAAAA");
                resources["BorderColor"] = Avalonia.Media.Color.Parse("#444444");
                resources["ShadowColor"] = Avalonia.Media.Color.Parse("#30000000");

                resources["BackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#121212"));
                resources["SurfaceBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#222222"));
                resources["TextPrimaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
                resources["TextSecondaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#AAAAAA"));
                resources["BorderBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#444444"));
                resources["ShadowBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#30000000"));
            }

            // Также установить градиент боковой панели
            var sidebarGradient = new Avalonia.Media.LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative)
            };

            if (IsLightTheme)
            {
                sidebarGradient.GradientStops.Add(new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#00ACC1"), 0));
                sidebarGradient.GradientStops.Add(new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#008999"), 1));
            }
            else
            {
                sidebarGradient.GradientStops.Add(new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#007D8C"), 0));
                sidebarGradient.GradientStops.Add(new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#005A66"), 1));
            }

            resources["SidebarGradient"] = sidebarGradient;

            // Установите тему для всего приложения
            Application.Current.RequestedThemeVariant = IsLightTheme ? Avalonia.Styling.ThemeVariant.Light : Avalonia.Styling.ThemeVariant.Dark;

            // Оповещаем подписчиков об изменении темы
            _themeChanged.OnNext(IsLightTheme);
        }

        private const string ThemeSettingKey = "AppTheme";
        public void SaveThemeSettings()
        {
            // Вы можете использовать любой механизм для сохранения
            // Например, AppSettings или файл JSON
            var settings = new JsonSerializerSettings();
            var themeSettings = new { IsLightTheme = _isLightTheme };
            File.WriteAllText("app_settings.json", JsonConvert.SerializeObject(themeSettings));
        }

        public void LoadThemeSettings()
        {
            try
            {
                if (File.Exists("app_settings.json"))
                {
                    var json = File.ReadAllText("app_settings.json");
                    var settings = JsonConvert.DeserializeObject<dynamic>(json);
                    _isLightTheme = settings.IsLightTheme;
                    ApplyTheme();
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                Console.WriteLine($"Error loading theme settings: {ex.Message}");
            }
        }
    }
}