using Avalonia;
using Avalonia.Media;
using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManufactPlanner.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private int _activeTasksCount;
        private int _activeOrdersCount;
        private int _deadlinesTodayCount;
        private List<TaskDetailsView> _recentTasks = new();

        public int ActiveTasksCount
        {
            get => _activeTasksCount;
            set => this.RaiseAndSetIfChanged(ref _activeTasksCount, value);
        }

        public int ActiveOrdersCount
        {
            get => _activeOrdersCount;
            set => this.RaiseAndSetIfChanged(ref _activeOrdersCount, value);
        }

        public int DeadlinesTodayCount
        {
            get => _deadlinesTodayCount;
            set => this.RaiseAndSetIfChanged(ref _deadlinesTodayCount, value);
        }

        public List<TaskDetailsView> RecentTasks
        {
            get => _recentTasks;
            set => this.RaiseAndSetIfChanged(ref _recentTasks, value);
        }

        public DashboardViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            ActiveTasksCount = _dbContext.Tasks.Count(t => t.Status == "В процессе" || t.Status == "В очереди");
            ActiveOrdersCount = _dbContext.Orders.Count(o => o.Status == "Активен");
            DeadlinesTodayCount = _dbContext.Tasks.Count(t => t.EndDate == DateOnly.FromDateTime(DateTime.Today));

            RecentTasks = _dbContext.TaskDetailsViews
                .OrderByDescending(t => t.EndDate)
                .Take(4)
                .ToList();
        }

        public string GetTaskDescription(TaskDetailsView task)
        {
            return $"{task.OrderNumber} поз. {task.PositionNumber}";
        }

        public IBrush GetTaskColor(TaskDetailsView task)
        {
            return task.Priority switch
            {
                1 => new SolidColorBrush(Color.Parse("#FF7043")),
                2 => new SolidColorBrush(Color.Parse("#FFB74D")),
                3 => new SolidColorBrush(Color.Parse("#4CAF9D")),
                _ => new SolidColorBrush(Color.Parse("#9575CD"))
            };
        }

        public string CurrentMonthYear => DateTime.Now.ToString("MMMM yyyy");
        public int CurrentDay => DateTime.Now.Day;

        public List<GraphPoint> TaskCompletionGraphPoints => new()
        {
            new GraphPoint { X = 0, Y = 60 },
            new GraphPoint { X = 30, Y = 70 },
            new GraphPoint { X = 60, Y = 50 },
            new GraphPoint { X = 90, Y = 40 },
            new GraphPoint { X = 120, Y = 10 },
            new GraphPoint { X = 150, Y = 20 },
            new GraphPoint { X = 180, Y = 0 }
        };

        public string TaskCompletionGraphPointsString =>
            string.Join(" ", TaskCompletionGraphPoints.Select(p => $"{p.X},{p.Y}"));
    }

    public class GraphPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}