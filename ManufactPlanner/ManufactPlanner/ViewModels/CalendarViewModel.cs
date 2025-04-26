using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class CalendarViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private string _currentPeriod = "Апрель 2025";
        private bool _isToday = true;
        private string[] _weekDays = { "7", "8", "9", "10", "11", "12", "13" };

        private ObservableCollection<CalendarEventViewModel> _mondayEvents;
        private ObservableCollection<CalendarEventViewModel> _tuesdayEvents;
        private ObservableCollection<CalendarEventViewModel> _wednesdayEvents;
        private ObservableCollection<CalendarEventViewModel> _thursdayEvents;
        private ObservableCollection<CalendarEventViewModel> _fridayEvents;

        public string CurrentPeriod
        {
            get => _currentPeriod;
            set => this.RaiseAndSetIfChanged(ref _currentPeriod, value);
        }

        public bool IsToday
        {
            get => _isToday;
            set => this.RaiseAndSetIfChanged(ref _isToday, value);
        }

        public string[] WeekDays
        {
            get => _weekDays;
            set => this.RaiseAndSetIfChanged(ref _weekDays, value);
        }

        public ObservableCollection<CalendarEventViewModel> MondayEvents
        {
            get => _mondayEvents;
            set => this.RaiseAndSetIfChanged(ref _mondayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> TuesdayEvents
        {
            get => _tuesdayEvents;
            set => this.RaiseAndSetIfChanged(ref _tuesdayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> WednesdayEvents
        {
            get => _wednesdayEvents;
            set => this.RaiseAndSetIfChanged(ref _wednesdayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> ThursdayEvents
        {
            get => _thursdayEvents;
            set => this.RaiseAndSetIfChanged(ref _thursdayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> FridayEvents
        {
            get => _fridayEvents;
            set => this.RaiseAndSetIfChanged(ref _fridayEvents, value);
        }

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand CreateEventCommand { get; }

        public CalendarViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            PreviousWeekCommand = ReactiveCommand.Create(PreviousWeek);
            NextWeekCommand = ReactiveCommand.Create(NextWeek);
            CreateEventCommand = ReactiveCommand.Create(CreateEvent);

            LoadEvents();
        }

        public CalendarViewModel()
        {
            // Конструктор для дизайнера
            PreviousWeekCommand = ReactiveCommand.Create(PreviousWeek);
            NextWeekCommand = ReactiveCommand.Create(NextWeek);
            CreateEventCommand = ReactiveCommand.Create(CreateEvent);

            LoadEvents();
        }

        private void LoadEvents()
        {
            // Примеры событий для разных дней недели
            MondayEvents = new ObservableCollection<CalendarEventViewModel>
            {
                new CalendarEventViewModel { Title = "Совещание", Time = "08:30-10:00", Color = "#9575CD", Duration = 80, Top = 30 }
            };

            TuesdayEvents = new ObservableCollection<CalendarEventViewModel>
            {
                new CalendarEventViewModel { Title = "Разработка", Time = "14:00-16:00", Color = "#4CAF9D", Duration = 100, Top = 260 }
            };

            WednesdayEvents = new ObservableCollection<CalendarEventViewModel>
            {
                new CalendarEventViewModel { Title = "Тестирование", Time = "10:00-12:00", Color = "#FFB74D", Duration = 120, Top = 100 }
            };

            ThursdayEvents = new ObservableCollection<CalendarEventViewModel>
            {
                new CalendarEventViewModel { Title = "Проект.", Time = "08:00-09:00", Color = "#4CAF9D", Duration = 60, Top = 0 },
                new CalendarEventViewModel { Title = "Разработка", Time = "11:00-14:00", Color = "#00ACC1", Duration = 180, Top = 120 }
            };

            FridayEvents = new ObservableCollection<CalendarEventViewModel>
            {
                new CalendarEventViewModel { Title = "Совещание", Time = "12:00-14:00", Color = "#FF7043", Duration = 120, Top = 160 }
            };
        }

        private void PreviousWeek()
        {
            // Логика перехода на предыдущую неделю
            IsToday = false;
        }

        private void NextWeek()
        {
            // Логика перехода на следующую неделю
            IsToday = false;
        }

        private void CreateEvent()
        {
            // Логика создания нового события в календаре
        }
    }

    public class CalendarEventViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string Time { get; set; }
        public string Color { get; set; }
        public int Duration { get; set; }
        public int Top { get; set; }
    }
}