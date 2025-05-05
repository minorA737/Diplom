using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using System.Reactive;
using ManufactPlanner.ViewModels.Dialogs;
using ManufactPlanner.Views.Dialogs;
using Avalonia.Controls;

namespace ManufactPlanner.ViewModels
{
    public class ProductionViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Основные свойства для отображения статистики
        private int _inProductionCount;
        private int _debuggingCount;
        private int _readyForPackagingCount;
        private ObservableCollection<ProductionItemViewModel> _productionItems;
        private bool _isLoading = true;
        private string _searchText = string.Empty;

        // Фильтры
        private ObservableCollection<string> _statuses = new ObservableCollection<string>();
        private ObservableCollection<string> _masters = new ObservableCollection<string>();
        private ObservableCollection<string> _periods = new ObservableCollection<string>();

        private string _selectedStatus;
        private string _selectedMaster;
        private string _selectedPeriod;

        // Пагинация
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize = 10;

        #region Свойства
        public int InProductionCount
        {
            get => _inProductionCount;
            set => this.RaiseAndSetIfChanged(ref _inProductionCount, value);
        }

        public int DebuggingCount
        {
            get => _debuggingCount;
            set => this.RaiseAndSetIfChanged(ref _debuggingCount, value);
        }

        public int ReadyForPackagingCount
        {
            get => _readyForPackagingCount;
            set => this.RaiseAndSetIfChanged(ref _readyForPackagingCount, value);
        }

        public ObservableCollection<ProductionItemViewModel> ProductionItems
        {
            get => _productionItems;
            set => this.RaiseAndSetIfChanged(ref _productionItems, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                // При изменении текста поиска - обновляем список
                RefreshProductionItems();
            }
        }

        // Свойства для фильтров
        public ObservableCollection<string> Statuses
        {
            get => _statuses;
            set => this.RaiseAndSetIfChanged(ref _statuses, value);
        }

        public ObservableCollection<string> Masters
        {
            get => _masters;
            set => this.RaiseAndSetIfChanged(ref _masters, value);
        }

        public ObservableCollection<string> Periods
        {
            get => _periods;
            set => this.RaiseAndSetIfChanged(ref _periods, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStatus, value);
                RefreshProductionItems();
            }
        }

        public string SelectedMaster
        {
            get => _selectedMaster;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMaster, value);
                RefreshProductionItems();
            }
        }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPeriod, value);
                RefreshProductionItems();
            }
        }

        // Свойства для пагинации
        public int CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => this.RaiseAndSetIfChanged(ref _totalPages, value);
        }

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;
        #endregion

        // Команды
        public ICommand RefreshCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ViewProductionDetailsCommand { get; }

        public ICommand CreateProductionOrderCommand { get; }

        public ProductionViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, Window parentWindow)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация команд
            RefreshCommand = ReactiveCommand.Create(RefreshProductionItems);

            NextPageCommand = ReactiveCommand.Create(() =>
            {
                if (CanGoToNextPage)
                {
                    CurrentPage++;
                    this.RaisePropertyChanged(nameof(CanGoToNextPage));
                    this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                    LoadProductionData();
                }
            });

            PreviousPageCommand = ReactiveCommand.Create(() =>
            {
                if (CanGoToPreviousPage)
                {
                    CurrentPage--;
                    this.RaisePropertyChanged(nameof(CanGoToNextPage));
                    this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                    LoadProductionData();
                }
            });

            ViewProductionDetailsCommand = ReactiveCommand.Create<int>((productionId) =>
            {
                // Тут можно реализовать переход на детальную информацию
                // _mainWindowViewModel.NavigateToProductionDetails(productionId);
            });
            
            CreateProductionOrderCommand = ReactiveCommand.CreateFromTask(async () => {
                // Создаем и показываем диалог
                var dialog = new ProductionOrderDialog(_dbContext);
                await dialog.ShowDialog(parentWindow); // Предполагаем, что в MainWindowViewModel есть свойство MainWindow

                // Проверяем результат диалога
                var dialogViewModel = dialog.DataContext as ProductionOrderDialogViewModel;
                if (dialogViewModel != null && dialogViewModel.DialogResult)
                {
                    // Обновляем список производственных заданий
                    RefreshProductionItems();
                }
            });
            // Инициализация фильтров
            InitializeFiltersAsync();

            // Загрузка данных из БД
            LoadProductionData();
        }

        private async void InitializeFiltersAsync()
        {
            try
            {
                // Инициализация фильтров статусов производства
                Statuses = new ObservableCollection<string>
                {
                    "Все статусы",
                    "Подготовка",
                    "Изготовление",
                    "Отладка",
                    "Завершено"
                };

                // Периоды
                Periods = new ObservableCollection<string>
                {
                    "Все периоды",
                    "Текущая неделя",
                    "Следующая неделя",
                    "Текущий месяц",
                    "Просроченные"
                };

                // Загрузка списка мастеров из базы данных
                var mastersFromDb = await _dbContext.ProductionDetails
                    .Where(p => p.MasterName != null)
                    .Select(p => p.MasterName)
                    .Distinct()
                    .ToListAsync();

                // Добавляем пункт "Все мастера" в начало списка
                Masters = new ObservableCollection<string>(new[] { "Все мастера" }.Concat(mastersFromDb));

                // Устанавливаем значения по умолчанию
                SelectedStatus = "Все статусы";
                SelectedMaster = "Все мастера";
                SelectedPeriod = "Все периоды";
            }
            catch (Exception)
            {
                // В случае ошибки инициализируем базовые значения
                Statuses = new ObservableCollection<string> { "Все статусы" };
                Masters = new ObservableCollection<string> { "Все мастера" };
                Periods = new ObservableCollection<string> { "Все периоды" };
            }
        }

        private async void LoadProductionData()
        {
            IsLoading = true;

            try
            {
                // Получаем статистику по производству
                InProductionCount = await _dbContext.ProductionDetails
                    .CountAsync(p => p.ProductionDate != null && p.AcceptanceDate == null);

                DebuggingCount = await _dbContext.ProductionDetails
                    .CountAsync(p => p.DebuggingDate != null && p.AcceptanceDate == null);

                ReadyForPackagingCount = await _dbContext.ProductionDetails
                    .CountAsync(p => p.AcceptanceDate != null && p.PackagingDate == null);

                // Создаем базовый запрос к производственным данным
                var query = _dbContext.ProductionDetails
                    .Include(p => p.OrderPosition)
                    .ThenInclude(op => op.Order)
                    .AsQueryable();

                // Применяем фильтры, если они выбраны
                if (!string.IsNullOrEmpty(SearchText))
                {
                    query = query.Where(p =>
                        p.OrderNumber.Contains(SearchText) ||
                        p.MasterName.Contains(SearchText) ||
                        p.OrderPosition.ProductName.Contains(SearchText));
                }

                if (!string.IsNullOrEmpty(SelectedMaster) && SelectedMaster != "Все мастера")
                {
                    query = query.Where(p => p.MasterName == SelectedMaster);
                }

                if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Все статусы")
                {
                    // Определяем условия фильтрации в зависимости от статуса
                    query = SelectedStatus switch
                    {
                        "Подготовка" => query.Where(p => p.ProductionDate == null),
                        "Изготовление" => query.Where(p => p.ProductionDate != null && p.DebuggingDate == null),
                        "Отладка" => query.Where(p => p.DebuggingDate != null && p.AcceptanceDate == null),
                        "Завершено" => query.Where(p => p.AcceptanceDate != null),
                        _ => query
                    };
                }

                if (!string.IsNullOrEmpty(SelectedPeriod) && SelectedPeriod != "Все периоды")
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var endOfWeek = DateOnly.FromDateTime(DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek));
                    var startOfNextWeek = endOfWeek.AddDays(1);
                    var endOfNextWeek = startOfNextWeek.AddDays(6);
                    var endOfMonth = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)));

                    query = SelectedPeriod switch
                    {
                        "Текущая неделя" => query.Where(p =>
                            (p.ProductionDate >= today && p.ProductionDate <= endOfWeek) ||
                            (p.DebuggingDate >= today && p.DebuggingDate <= endOfWeek) ||
                            (p.AcceptanceDate >= today && p.AcceptanceDate <= endOfWeek)),
                        "Следующая неделя" => query.Where(p =>
                            (p.ProductionDate >= startOfNextWeek && p.ProductionDate <= endOfNextWeek) ||
                            (p.DebuggingDate >= startOfNextWeek && p.DebuggingDate <= endOfNextWeek) ||
                            (p.AcceptanceDate >= startOfNextWeek && p.AcceptanceDate <= endOfNextWeek)),
                        "Текущий месяц" => query.Where(p =>
                            (p.ProductionDate >= today && p.ProductionDate <= endOfMonth) ||
                            (p.DebuggingDate >= today && p.DebuggingDate <= endOfMonth) ||
                            (p.AcceptanceDate >= today && p.AcceptanceDate <= endOfMonth)),
                        "Просроченные" => query.Where(p =>
                            (p.ProductionDate < today && p.DebuggingDate == null) ||
                            (p.DebuggingDate < today && p.AcceptanceDate == null)),
                        _ => query
                    };
                }

                // Подсчитываем общее количество элементов для пагинации
                var totalCount = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(totalCount / (double)_pageSize);

                // Проверяем текущую страницу
                if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = TotalPages;
                }
                else if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }

                // Применяем пагинацию
                var pagedItems = await query
                    .OrderByDescending(p => p.ProductionDate)
                    .Skip((CurrentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToListAsync();

                // Преобразуем в ViewModel
                var productionViewModels = pagedItems.Select((p, index) =>
                {
                    // Определяем статус и его цвет
                    string status = GetProductionStatus(p);
                    string statusColor = GetStatusColor(status);

                    // Вычисляем прогресс выполнения
                    int progressPercent = CalculateProgressPercent(p);

                    return new ProductionItemViewModel
                    {
                        Id = p.Id,
                        OrderNumber = p.OrderNumber ?? "№ не указан",
                        Name = p.OrderPosition?.ProductName ?? "Не указано",
                        OrderReference = p.OrderPosition?.Order?.OrderNumber + " поз. " + p.OrderPosition?.PositionNumber,
                        Master = p.MasterName ?? "Не назначен",
                        StartDate = p.ProductionDate?.ToString("dd.MM.yyyy") ?? "Не начато",
                        EndDate = p.PackagingDate?.ToString("dd.MM.yyyy") ?? p.AcceptanceDate?.ToString("dd.MM.yyyy") ?? "Не завершено",
                        Status = status,
                        StatusColor = statusColor,
                        Progress = $"{progressPercent}%",
                        ProgressWidth = CalculateProgressWidth(progressPercent),
                        IsAlternate = index % 2 == 1
                    };
                }).ToList();

                // Обновляем коллекцию
                ProductionItems = new ObservableCollection<ProductionItemViewModel>(productionViewModels);

                // Обновляем свойства пагинации
                this.RaisePropertyChanged(nameof(CanGoToNextPage));
                this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
            }
            catch (Exception ex)
            {
                // В случае ошибки показываем пустой список
                ProductionItems = new ObservableCollection<ProductionItemViewModel>();

                // Тут можно добавить логирование ошибки
                // Console.WriteLine($"Ошибка при загрузке данных производства: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshProductionItems()
        {
            // Сбрасываем на первую страницу при обновлении списка
            CurrentPage = 1;
            LoadProductionData();
        }

        // Вспомогательные методы для вычисления статуса и прогресса
        private string GetProductionStatus(ProductionDetail productionDetail)
        {
            if (productionDetail.AcceptanceDate != null)
                return "Завершено";
            if (productionDetail.DebuggingDate != null)
                return "Отладка";
            if (productionDetail.ProductionDate != null)
                return "Изготовление";
            return "Подготовка";
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Завершено" => "#00ACC1",  // Голубой
                "Отладка" => "#4CAF9D",    // Зеленый
                "Изготовление" => "#9575CD", // Фиолетовый
                "Подготовка" => "#FFB74D",  // Оранжевый
                _ => "#666666"             // Серый по умолчанию
            };
        }

        private int CalculateProgressPercent(ProductionDetail productionDetail)
        {
            if (productionDetail.PackagingDate != null)
                return 100;
            if (productionDetail.AcceptanceDate != null)
                return 90;
            if (productionDetail.DebuggingDate != null)
                return 60;
            if (productionDetail.ProductionDate != null)
                return 30;
            return 10;
        }

        private int CalculateProgressWidth(int progressPercent)
        {
            // Преобразуем процент в ширину полосы прогресса (максимум 120px)
            return (int)(progressPercent * 1.2);
        }
    }

    public class ProductionItemViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Name { get; set; }
        public string OrderReference { get; set; }
        public string Master { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string Progress { get; set; }
        public int ProgressWidth { get; set; }
        public bool IsAlternate { get; set; }
    }
}