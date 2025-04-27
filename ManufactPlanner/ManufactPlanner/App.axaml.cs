using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ManufactPlanner.ViewModels;
using ManufactPlanner.Views;
using Avalonia.Data.Core.Plugins;
using ManufactPlanner.Models;

namespace ManufactPlanner
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Удаляем DataAnnotations validator для улучшения производительности
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Добавляем ресурсы для навигационной панели и других элементов
                if (Current != null)
                {
                    var resources = new ResourceDictionary();
                    var sidebarGradient = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
                    };
                    sidebarGradient.GradientStops.Add(new GradientStop { Color = Color.Parse("#00ACC1"), Offset = 0 });
                    sidebarGradient.GradientStops.Add(new GradientStop { Color = Color.Parse("#008999"), Offset = 1 });
                    resources.Add("SidebarGradient", sidebarGradient);

                    // Добавляем цвета для приложения
                    resources.Add("PrimaryColor", Color.Parse("#00ACC1"));
                    resources.Add("SecondaryColor", Color.Parse("#9575CD"));
                    resources.Add("SuccessColor", Color.Parse("#4CAF9D"));
                    resources.Add("WarningColor", Color.Parse("#FFB74D"));
                    resources.Add("ErrorColor", Color.Parse("#FF7043"));
                    resources.Add("BackgroundColor", Color.Parse("#F8F9FA"));
                    Current.Resources.MergedDictionaries.Add(resources);
                }

                // Создаем экземпляр DbContext
                var dbContext = new PostgresContext();

                // Создаем MainWindow с AuthPage в качестве начального контента
                var mainViewModel = new MainWindowViewModel();

                // Создаем главное окно и устанавливаем DataContext
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // Устанавливаем AuthPage как начальное представление
                mainViewModel.CurrentView = new AuthPage(mainViewModel, dbContext);

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}