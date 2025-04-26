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
    public class DocumentationViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private ObservableCollection<DocumentViewModel> _documents;

        public ObservableCollection<DocumentViewModel> Documents
        {
            get => _documents;
            set => this.RaiseAndSetIfChanged(ref _documents, value);
        }

        public ICommand DownloadCommand { get; }
        public ICommand ViewCommand { get; }

        public DocumentationViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            DownloadCommand = ReactiveCommand.Create<int>(DownloadDocument);
            ViewCommand = ReactiveCommand.Create<int>(ViewDocument);

            LoadDocuments();
        }

        public DocumentationViewModel()
        {
            // Конструктор для дизайнера
            DownloadCommand = ReactiveCommand.Create<int>(DownloadDocument);
            ViewCommand = ReactiveCommand.Create<int>(ViewDocument);

            LoadDocuments();
        }

        private void LoadDocuments()
        {
            // В реальном приложении здесь будет загрузка данных из БД
            // Для примера используем тестовые данные
            Documents = new ObservableCollection<DocumentViewModel>
            {
                new DocumentViewModel { Id = 1, Name = "Техническое задание на разработку стенда", Description = "Версия 1.2", Type = "Техническое задание", OrderNumber = "ОП-113/24", CreatedDate = "12.03.2025", Author = "Конев Т.У.", IsAlternate = false },
                new DocumentViewModel { Id = 2, Name = "Спецификация компонентов", Description = "Схема электрическая", Type = "Спецификация", OrderNumber = "ОП-136/24", CreatedDate = "15.03.2025", Author = "Вяткин А.И.", IsAlternate = true },
                new DocumentViewModel { Id = 3, Name = "Руководство по эксплуатации", Description = "Финальная версия", Type = "Руководство", OrderNumber = "ОП-136/24", CreatedDate = "20.03.2025", Author = "Теплов В.Ф.", IsAlternate = false },
                new DocumentViewModel { Id = 4, Name = "Сборочный чертеж", Description = "Версия 2.0", Type = "Чертеж", OrderNumber = "ОП-168/24", CreatedDate = "25.03.2025", Author = "Еретин Д.К.", IsAlternate = true },
                new DocumentViewModel { Id = 5, Name = "Монтажная схема", Description = "Финальная версия", Type = "Схема", OrderNumber = "ОП-168/24", CreatedDate = "28.03.2025", Author = "Турушев С.М.", IsAlternate = false }
            };
        }

        private void DownloadDocument(int documentId)
        {
            // Логика скачивания документа
        }

        private void ViewDocument(int documentId)
        {
            // Логика просмотра документа
        }
    }

    public class DocumentViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string OrderNumber { get; set; }
        public string CreatedDate { get; set; }
        public string Author { get; set; }
        public bool IsAlternate { get; set; }
    }
}