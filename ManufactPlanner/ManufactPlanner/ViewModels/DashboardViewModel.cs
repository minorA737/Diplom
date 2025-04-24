using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ManufactPlanner.Models;
using ReactiveUI;

namespace ManufactPlanner.ViewModels.Dashboard
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Здесь добавьте свойства для дашборда

        public DashboardViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Загрузка данных для дашборда
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            // Здесь загружаем данные из базы
            // Например, активные задачи, статистику и т.д.
        }
    }
}
