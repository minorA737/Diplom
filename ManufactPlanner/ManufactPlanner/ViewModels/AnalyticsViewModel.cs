using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManufactPlanner.ViewModels
{
    public class AnalyticsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private DateTime _startDate = DateTime.Now.AddMonths(-6);
        private DateTime _endDate = DateTime.Now;
        private int _selectedReportType = 0;

        public DateTime StartDate
        {
            get => _startDate;
            set => this.RaiseAndSetIfChanged(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => this.RaiseAndSetIfChanged(ref _endDate, value);
        }

        public int SelectedReportType
        {
            get => _selectedReportType;
            set => this.RaiseAndSetIfChanged(ref _selectedReportType, value);
        }

        public AnalyticsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
        }

        public AnalyticsViewModel()
        {
            // Конструктор для дизайнера
        }
    }
}