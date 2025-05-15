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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

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
        private ObservableCollection<ProductionItemViewModel> _productionItems = new();
        private bool _isLoading = true;
        private string _searchText = string.Empty;

        // Фильтры
        private ObservableCollection<string> _statuses = new();
        private ObservableCollection<string> _masters = new();
        private ObservableCollection<string> _periods = new();

        private string _selectedStatus = "Все статусы";
        private string _selectedMaster = "Все мастера";
        private string _selectedPeriod = "Все периоды";

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
                _ = RefreshProductionItemsAsync();
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
                _ = RefreshProductionItemsAsync();
            }
        }

        public string SelectedMaster
        {
            get => _selectedMaster;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMaster, value);
                _ = RefreshProductionItemsAsync();
            }
        }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPeriod, value);
                _ = RefreshProductionItemsAsync();
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
        public ICommand RefreshCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        public ICommand PreviousPageCommand { get; private set; }
        public ICommand ViewProductionDetailsCommand { get; private set; }
        public ICommand CreateProductionOrderCommand { get; private set; }

        public ProductionViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация команд (должна быть первой)
            InitializeCommands();

            // Инициализация коллекций с пустыми значениями
            ProductionItems = new ObservableCollection<ProductionItemViewModel>();
            InitializeEmptyCollections();

            // Запускаем асинхронную инициализацию
            _ = InitializeAsync();
        }

        private void InitializeCommands()
        {
            RefreshCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await RefreshProductionItemsAsync();
            });

            CreateProductionOrderCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await CreateProductionOrder();
            });

            NextPageCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (CanGoToNextPage)
                {
                    CurrentPage++;
                    this.RaisePropertyChanged(nameof(CanGoToNextPage));
                    this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                    await LoadProductionDataAsync();
                }
            });

            PreviousPageCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (CanGoToPreviousPage)
                {
                    CurrentPage--;
                    this.RaisePropertyChanged(nameof(CanGoToNextPage));
                    this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                    await LoadProductionDataAsync();
                }
            });

            ViewProductionDetailsCommand = ReactiveCommand.Create<int>((productionId) =>
            {
                // Реализация перехода на детальную информацию
                // _mainWindowViewModel.NavigateToProductionDetails(productionId);
            });
        }

        private void InitializeEmptyCollections()
        {
            // Инициализируем пустые коллекции, чтобы избежать проблем с привязкой
            Statuses = new ObservableCollection<string> { "Все статусы" };
            Masters = new ObservableCollection<string> { "Все мастера" };
            Periods = new ObservableCollection<string> { "Все периоды" };
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                await InitializeFiltersAsync();
                await LoadProductionDataAsync();
            }
            catch (Exception ex)
            {
                // Обработка ошибок инициализации
                Console.WriteLine($"Ошибка инициализации: {ex.Message}");
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task InitializeFiltersAsync()
        {
            try
            {
                // Инициализация фильтров статусов производства
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Statuses.Clear();
                    Statuses.Add("Все статусы");
                    Statuses.Add("Подготовка");
                    Statuses.Add("Изготовление");
                    Statuses.Add("Отладка");
                    Statuses.Add("Завершено");

                    // Периоды
                    Periods.Clear();
                    Periods.Add("Все периоды");
                    Periods.Add("Текущая неделя");
                    Periods.Add("Следующая неделя");
                    Periods.Add("Текущий месяц");
                    Periods.Add("Просроченные");
                });

                // Загрузка списка мастеров из базы данных
                var mastersFromDb = await _dbContext.ProductionDetails
                    .Where(p => !string.IsNullOrEmpty(p.MasterName))
                    .Select(p => p.MasterName)
                    .Distinct()
                    .ToListAsync();

                // Обновляем UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Masters.Clear();
                    Masters.Add("Все мастера");
                    foreach (var master in mastersFromDb)
                    {
                        Masters.Add(master);
                    }
                });

                // Устанавливаем значения по умолчанию
                SelectedStatus = "Все статусы";
                SelectedMaster = "Все мастера";
                SelectedPeriod = "Все периоды";
            }
            catch (Exception ex)
            {
                // В случае ошибки просто оставляем значения по умолчанию
                Console.WriteLine($"Ошибка при загрузке фильтров: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task CreateProductionOrder()
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null)
                {
                    Console.WriteLine("Не удалось получить главное окно приложения");
                    return;
                }

                // Создаем диалог создания заказ-наряда
                var dialog = new ProductionOrderDialog(_dbContext);

                // Показываем диалог и ждем его закрытия
                bool? result = await dialog.ShowDialog<bool?>(mainWindow);

                // Если диалог был закрыт с положительным результатом, обновляем данные
                if (result == true)
                {
                    await RefreshProductionItemsAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании заказ-наряда: {ex.Message}");
            }
        }

        private Window GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        private async System.Threading.Tasks.Task LoadProductionDataAsync()
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

                // Применяем фильтры
                query = ApplyFilters(query);

                // Подсчитываем общее количество элементов для пагинации
                var totalCount = await query.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)_pageSize));

                // Проверяем текущую страницу
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }
                else if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }

                // Применяем пагинацию
                var pagedItems = await query
                    .OrderByDescending(p => p.ProductionDate ?? DateOnly.MinValue)
                    .ThenByDescending(p => p.Id)
                    .Skip(Math.Max(0, (CurrentPage - 1) * _pageSize))
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
                        OrderReference = (p.OrderPosition?.Order?.OrderNumber ?? "№ не указан") + " поз. " + (p.OrderPosition?.PositionNumber ?? ""),
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

                // Обновляем коллекцию в UI потоке
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProductionItems.Clear();
                    foreach (var item in productionViewModels)
                    {
                        ProductionItems.Add(item);
                    }
                });

                // Обновляем свойства пагинации
                this.RaisePropertyChanged(nameof(CanGoToNextPage));
                this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
            }
            catch (Exception ex)
            {
                // В случае ошибки показываем пустой список
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProductionItems.Clear();
                });

                Console.WriteLine($"Ошибка при загрузке данных производства: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private IQueryable<ProductionDetail> ApplyFilters(IQueryable<ProductionDetail> query)
        {
            // Применяем фильтр поиска
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(p =>
                    (p.OrderNumber != null && p.OrderNumber.Contains(SearchText)) ||
                    (p.MasterName != null && p.MasterName.Contains(SearchText)) ||
                    (p.OrderPosition != null && p.OrderPosition.ProductName != null && p.OrderPosition.ProductName.Contains(SearchText)));
            }

            // Применяем фильтр мастера
            if (!string.IsNullOrEmpty(SelectedMaster) && SelectedMaster != "Все мастера")
            {
                query = query.Where(p => p.MasterName == SelectedMaster);
            }

            // Применяем фильтр статуса
            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Все статусы")
            {
                query = SelectedStatus switch
                {
                    "Подготовка" => query.Where(p => p.ProductionDate == null),
                    "Изготовление" => query.Where(p => p.ProductionDate != null && p.DebuggingDate == null),
                    "Отладка" => query.Where(p => p.DebuggingDate != null && p.AcceptanceDate == null),
                    "Завершено" => query.Where(p => p.AcceptanceDate != null),
                    _ => query
                };
            }

            // Применяем фильтр периода
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

            return query;
        }

        private async System.Threading.Tasks.Task RefreshProductionItemsAsync()
        {
            // Сбрасываем на первую страницу при обновлении списка
            CurrentPage = 1;
            await LoadProductionDataAsync();
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
        public string OrderNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OrderReference { get; set; } = string.Empty;
        public string Master { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string Progress { get; set; } = string.Empty;
        public int ProgressWidth { get; set; }
        public bool IsAlternate { get; set; }
    }
}