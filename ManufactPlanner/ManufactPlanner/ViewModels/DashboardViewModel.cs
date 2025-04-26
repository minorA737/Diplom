using ManufactPlanner.Models;
using ReactiveUI;

namespace ManufactPlanner.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private int _activeTasksCount = 28;
        private int _activeOrdersCount = 14;
        private int _deadlinesTodayCount = 5;

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

        public DashboardViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Загрузка данных из базы данных (здесь используются тестовые данные)
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            // В реальном приложении здесь будет логика загрузки данных из базы данных
            // Например:
            // ActiveTasksCount = _dbContext.Tasks.Count(t => t.Status == "В процессе" || t.Status == "В очереди");
            // ActiveOrdersCount = _dbContext.Orders.Count(o => o.Status == "Активен");
            // DeadlinesTodayCount = _dbContext.Tasks.Count(t => t.EndDate.Date == DateTime.Today);
        }
    }
}