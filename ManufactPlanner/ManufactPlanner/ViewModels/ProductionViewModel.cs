using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManufactPlanner.ViewModels
{
    public class ProductionViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private int _inProductionCount = 8;
        private int _debuggingCount = 3;
        private int _readyForPackagingCount = 2;
        private ObservableCollection<ProductionItemViewModel> _productionItems;

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

        public ProductionViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            LoadProductionData();
        }

        public ProductionViewModel()
        {
            // Конструктор для дизайнера
            LoadProductionData();
        }

        private void LoadProductionData()
        {
            // В реальном приложении здесь будет загрузка данных из БД
            // Для примера используем тестовые данные
            ProductionItems = new ObservableCollection<ProductionItemViewModel>
            {
                new ProductionItemViewModel { OrderNumber = "125/168-1.2", Name = "Стенд учебный КИП", OrderReference = "ОП-168/24 поз. 1.2", Master = "БР4 Васильев / РЭМ", StartDate = "21.03.2025", EndDate = "27.03.2025", Status = "Изготовление", StatusColor = "#9575CD", Progress = "45%", ProgressWidth = 54, IsAlternate = false },
                new ProductionItemViewModel { OrderNumber = "173/168-1.7", Name = "Стенд Силовая электроника", OrderReference = "ОП-168/24 поз. 1.7", Master = "БР4 Васильев / РЭМ", StartDate = "14.04.2025", EndDate = "21.04.2025", Status = "Отладка", StatusColor = "#4CAF9D", Progress = "75%", ProgressWidth = 90, IsAlternate = true },
                new ProductionItemViewModel { OrderNumber = "140/168-1.8", Name = "Стенд Электротехника", OrderReference = "ОП-168/24 поз. 1.8", Master = "БР4 Васильев / РЭМ1 Шулепов", StartDate = "25.02.2025", EndDate = "28.02.2025", Status = "Завершено", StatusColor = "#00ACC1", Progress = "100%", ProgressWidth = 120, IsAlternate = false },
                new ProductionItemViewModel { OrderNumber = "139/168-1.14", Name = "Стенд Эл. станции", OrderReference = "ОП-168/24 поз. 1.14", Master = "БР4 Васильев / РЭМ Шулепов", StartDate = "22.04.2025", EndDate = "29.04.2025", Status = "Подготовка", StatusColor = "#FFB74D", Progress = "15%", ProgressWidth = 18, IsAlternate = true },
                new ProductionItemViewModel { OrderNumber = "141/169-1", Name = "Тренажер АРМ бурильщика", OrderReference = "ОП-169/24 поз. 1", Master = "РЭМ2 Дикарев / БР2 Полежаев", StartDate = "17.04.2025", EndDate = "26.04.2025", Status = "Отладка", StatusColor = "#4CAF9D", Progress = "60%", ProgressWidth = 72, IsAlternate = false }
            };
        }
    }

    public class ProductionItemViewModel : ViewModelBase
    {
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