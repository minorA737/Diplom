using ReactiveUI;

namespace ManufactPlanner.ViewModels
{
    public class EmployeeLoadViewModel : ViewModelBase
    {
        private string _employeeName;
        private int _loadPercent;
        private int _totalTasks;
        private int _completedTasks;
        private int _inProgressTasks;

        public string EmployeeName
        {
            get => _employeeName;
            set => this.RaiseAndSetIfChanged(ref _employeeName, value);
        }

        public int LoadPercent
        {
            get => _loadPercent;
            set => this.RaiseAndSetIfChanged(ref _loadPercent, value);
        }

        public int TotalTasks
        {
            get => _totalTasks;
            set => this.RaiseAndSetIfChanged(ref _totalTasks, value);
        }

        public int CompletedTasks
        {
            get => _completedTasks;
            set => this.RaiseAndSetIfChanged(ref _completedTasks, value);
        }

        public int InProgressTasks
        {
            get => _inProgressTasks;
            set => this.RaiseAndSetIfChanged(ref _inProgressTasks, value);
        }

        // Свойство для вычисления ширины прогресс-бара (основывается на загрузке в %)
        public int LoadBarWidth => (int)(LoadPercent * 3.2); // 320 пикселей = 100%
    }
}