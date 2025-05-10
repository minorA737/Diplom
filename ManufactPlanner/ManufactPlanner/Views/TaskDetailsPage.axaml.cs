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

        // �������� ������� ���� �� ����������� ���������
        var parentWindow = AppWindows.MainWindow;
        System.Diagnostics.Debug.WriteLine($"���������� ������� ����: {parentWindow != null}");

        // �������� �������� ������������ �� MainWindowViewModel
        var currentUser = GetCurrentUserFromMainViewModel(mainWindowViewModel, dbContext);
        System.Diagnostics.Debug.WriteLine($"������� ������� ������������: {currentUser}");

        DataContext = new TaskDetailsViewModel(mainWindowViewModel, dbContext, taskId, parentWindow, currentUser);
    }

    private Guid GetCurrentUserFromMainViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        try
        {
            // ���������, ���� �� � MainWindowViewModel ���������� � ������� ������������
            // ��� ����� ����������� � MainWindowViewModel ��� �������� CurrentUserId
            if (mainWindowViewModel != null && mainWindowViewModel.GetType().GetProperty("CurrentUserId") != null)
            {
                var currentUserId = (Guid)mainWindowViewModel.GetType().GetProperty("CurrentUserId").GetValue(mainWindowViewModel);
                if (currentUserId != Guid.Empty)
                {
                    return currentUserId;
                }
            }

            // ���� �� ������� �������� �� MainViewModel, ���� � ���������� ����������
            // ��������, � ��� ���� ����� AppSettings ��� �������

            // ��������� ������� - ������ ���������� ������������ admin
            var user = dbContext.Users.FirstOrDefault(u => u.Username == "admin");
            if (user != null)
            {
                return user.Id;
            }

            // ���� �� ������, ������ ������ Guid
            return Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}