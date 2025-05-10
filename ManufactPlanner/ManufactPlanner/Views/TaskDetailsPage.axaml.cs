using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using System;
using System.Linq;

namespace ManufactPlanner.Views;

public partial class TaskDetailsPage : UserControl
{
    public TaskDetailsPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int taskId)
    {
        InitializeComponent();

        // Получаем главное окно из глобального хранилища
        var parentWindow = AppWindows.MainWindow;
        System.Diagnostics.Debug.WriteLine($"Глобальное главное окно: {parentWindow != null}");

        // Получаем текущего пользователя из MainWindowViewModel
        var currentUser = GetCurrentUserFromMainViewModel(mainWindowViewModel, dbContext);
        System.Diagnostics.Debug.WriteLine($"Получен текущий пользователь: {currentUser}");

        DataContext = new TaskDetailsViewModel(mainWindowViewModel, dbContext, taskId, parentWindow, currentUser);
    }

    private Guid GetCurrentUserFromMainViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        try
        {
            // Проверяем, есть ли в MainWindowViewModel информация о текущем пользователе
            // Это нужно реализовать в MainWindowViewModel как свойство CurrentUserId
            if (mainWindowViewModel != null && mainWindowViewModel.GetType().GetProperty("CurrentUserId") != null)
            {
                var currentUserId = (Guid)mainWindowViewModel.GetType().GetProperty("CurrentUserId").GetValue(mainWindowViewModel);
                if (currentUserId != Guid.Empty)
                {
                    return currentUserId;
                }
            }

            // Если не удалось получить из MainViewModel, ищем в настройках приложения
            // Возможно, у вас есть класс AppSettings или похожий

            // Временное решение - всегда возвращаем пользователя admin
            var user = dbContext.Users.FirstOrDefault(u => u.Username == "admin");
            if (user != null)
            {
                return user.Id;
            }

            // Если не найден, вернем пустой Guid
            return Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}