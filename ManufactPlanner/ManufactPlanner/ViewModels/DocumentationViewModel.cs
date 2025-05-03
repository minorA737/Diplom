using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ManufactPlanner.ViewModels
{
    public class DocumentationViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private ObservableCollection<DocumentViewModel> _documents;
        private bool _isLoading = false;
        private string _searchText = string.Empty;
        private int _selectedTypeIndex = 0;
        private string _statusMessage = string.Empty;
        private bool _showStatusMessage = false;

        public ObservableCollection<DocumentViewModel> Documents
        {
            get => _documents;
            set => this.RaiseAndSetIfChanged(ref _documents, value);
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
                FilterDocuments();
            }
        }

        public int SelectedTypeIndex
        {
            get => _selectedTypeIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTypeIndex, value);
                FilterDocuments();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool ShowStatusMessage
        {
            get => _showStatusMessage;
            set => this.RaiseAndSetIfChanged(ref _showStatusMessage, value);
        }

        public ICommand DownloadCommand { get; }
        public ICommand ViewCommand { get; }
        public ICommand AddDocumentCommand { get; }
        public ICommand RefreshCommand { get; }

        private ObservableCollection<DocumentViewModel> _allDocuments;

        public DocumentationViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            DownloadCommand = ReactiveCommand.CreateFromTask<int>(DownloadDocumentAsync);
            ViewCommand = ReactiveCommand.CreateFromTask<int>(ViewDocumentAsync);
            AddDocumentCommand = ReactiveCommand.Create(AddDocument);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadDocumentsAsync);

            // Инициализация коллекции
            Documents = new ObservableCollection<DocumentViewModel>();

            // Асинхронно загружаем документы
            LoadDocumentsAsync().ConfigureAwait(false);
        }

        // Конструктор для дизайнера
        public DocumentationViewModel()
        {
            DownloadCommand = ReactiveCommand.CreateFromTask<int>(DownloadDocumentAsync);
            ViewCommand = ReactiveCommand.CreateFromTask<int>(ViewDocumentAsync);
            AddDocumentCommand = ReactiveCommand.Create(AddDocument);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadDocumentsAsync);

            // Для отображения в дизайнере
            LoadSampleData();
        }

        private async System.Threading.Tasks.Task LoadDocumentsAsync()
        {
            try
            {
                IsLoading = true;

                if (_dbContext != null)
                {
                    // Загрузка данных из базы
                    var designDocs = await _dbContext.DesignDocumentations
                        .Include(d => d.OrderPosition)
                            .ThenInclude(op => op.Order)
                        .ToListAsync();

                    var result = new List<DocumentViewModel>();
                    int counter = 0;

                    foreach (var doc in designDocs)
                    {
                        var orderNumber = doc.OrderPosition?.Order?.OrderNumber ?? "Не указан";
                        var productName = doc.OrderPosition?.ProductName ?? "Не указан";

                        // Определяем типы документации и даты создания
                        AddDocumentIfExists(result, doc.TechnicalTaskDate, "Техническое задание", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.ComponentListDate, "Спецификация", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.ProductCompositionDate, "Состав изделия", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.OperationManualDate, "Руководство по эксплуатации", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.ManualDate, "Методичка", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.PassportDate, "Паспорт", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.DesignDocsDate, "Конструкторская документация", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.PrintingFileDate, "Файл печати", orderNumber, productName, counter++);
                        AddDocumentIfExists(result, doc.SoftwareDate, "Программное обеспечение", orderNumber, productName, counter++);
                    }

                    _allDocuments = new ObservableCollection<DocumentViewModel>(result);
                    Documents = new ObservableCollection<DocumentViewModel>(_allDocuments);
                }
                else
                {
                    LoadSampleData();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки документов: {ex.Message}";
                ShowStatusMessage = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddDocumentIfExists(List<DocumentViewModel> list, DateOnly? date, string type, string orderNumber, string productName, int id)
        {
            if (date.HasValue)
            {
                list.Add(new DocumentViewModel
                {
                    Id = id,
                    Name = $"{type} - {productName}",
                    Description = $"Создан {date.Value.ToString("dd.MM.yyyy")}",
                    Type = type,
                    OrderNumber = orderNumber,
                    CreatedDate = date.Value.ToString("dd.MM.yyyy"),
                    Author = "Система",
                    IsAlternate = (id % 2 == 0)
                });
            }
        }

        private void LoadSampleData()
        {
            var sampleData = new List<DocumentViewModel>
            {
                new DocumentViewModel { Id = 1, Name = "Техническое задание на разработку стенда", Description = "Версия 1.2", Type = "Техническое задание", OrderNumber = "ОП-113/24", CreatedDate = "12.03.2025", Author = "Конев Т.У.", IsAlternate = false },
                new DocumentViewModel { Id = 2, Name = "Спецификация компонентов", Description = "Схема электрическая", Type = "Спецификация", OrderNumber = "ОП-136/24", CreatedDate = "15.03.2025", Author = "Вяткин А.И.", IsAlternate = true },
                new DocumentViewModel { Id = 3, Name = "Руководство по эксплуатации", Description = "Финальная версия", Type = "Руководство", OrderNumber = "ОП-136/24", CreatedDate = "20.03.2025", Author = "Теплов В.Ф.", IsAlternate = false },
                new DocumentViewModel { Id = 4, Name = "Сборочный чертеж", Description = "Версия 2.0", Type = "Чертеж", OrderNumber = "ОП-168/24", CreatedDate = "25.03.2025", Author = "Еретин Д.К.", IsAlternate = true },
                new DocumentViewModel { Id = 5, Name = "Монтажная схема", Description = "Финальная версия", Type = "Схема", OrderNumber = "ОП-168/24", CreatedDate = "28.03.2025", Author = "Турушев С.М.", IsAlternate = false }
            };

            _allDocuments = new ObservableCollection<DocumentViewModel>(sampleData);
            Documents = new ObservableCollection<DocumentViewModel>(_allDocuments);
        }

        private void FilterDocuments()
        {
            if (_allDocuments == null) return;

            var filteredDocs = _allDocuments.AsEnumerable();

            // Фильтрация по тексту поиска
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filteredDocs = filteredDocs.Where(d =>
                    d.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    d.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    d.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Фильтрация по типу
            if (SelectedTypeIndex > 0)
            {
                string selectedType = GetSelectedType();
                if (!string.IsNullOrEmpty(selectedType))
                {
                    filteredDocs = filteredDocs.Where(d => d.Type == selectedType);
                }
            }

            Documents = new ObservableCollection<DocumentViewModel>(filteredDocs);
        }

        private string GetSelectedType()
        {
            return SelectedTypeIndex switch
            {
                1 => "Техническое задание",
                2 => "Спецификация",
                3 => "Руководство",
                4 => "Чертеж",
                5 => "Схема",
                _ => string.Empty
            };
        }

        private async System.Threading.Tasks.Task DownloadDocumentAsync(int documentId)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Создание документа...";
                ShowStatusMessage = true;

                var document = _allDocuments.FirstOrDefault(d => d.Id == documentId);
                if (document == null)
                {
                    StatusMessage = "Документ не найден";
                    return;
                }

                // Здесь должна быть логика создания PDF
                string fileName = $"{document.Type}_{document.OrderNumber}_{DateTime.Now:yyyyMMdd}.pdf";
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, fileName);

                // Генерируем PDF с помощью QuestPDF
                await System.Threading.Tasks.Task.Run(() => {
                    QuestPDF.Settings.License = LicenseType.Community;

                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(12));

                            page.Header()
                                .Text($"{document.OrderNumber} - {document.Type}")
                                .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                            page.Content()
                                .PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
                                .Column(x =>
                                {
                                    x.Spacing(20);

                                    x.Item().Text($"Наименование: {document.Name}").FontSize(14);
                                    x.Item().Text($"Описание: {document.Description}");
                                    x.Item().Text($"Дата создания: {document.CreatedDate}");
                                    x.Item().Text($"Автор: {document.Author}");

                                    // Здесь нужно будет добавить содержимое документа из БД
                                    x.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
                                    {
                                        column.Item().Text("Содержимое документа:").Bold();
                                        column.Item().Text("Документ представляет собой техническую спецификацию для разработки изделия.");
                                        column.Item().Text("Включает в себя описание требований, параметры и характеристики.");
                                    });
                                });

                            page.Footer()
                                .AlignCenter()
                                .Text(x =>
                                {
                                    x.Span("Страница ");
                                    x.CurrentPageNumber();
                                    x.Span(" из ");
                                    x.TotalPages();
                                    x.Span($" - Создано: {DateTime.Now:dd.MM.yyyy HH:mm}");
                                });
                        });
                    })
                    .GeneratePdf(filePath);
                });

                StatusMessage = $"Документ сохранен: {filePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при создании документа: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                ShowStatusMessage = true;
            }
        }

        private async System.Threading.Tasks.Task ViewDocumentAsync(int documentId)
        {
            try
            {
                // Сначала создаем PDF
                await DownloadDocumentAsync(documentId);

                // Затем открываем его через системное приложение
                var document = _allDocuments.FirstOrDefault(d => d.Id == documentId);
                if (document != null)
                {
                    string fileName = $"{document.Type}_{document.OrderNumber}_{DateTime.Now:yyyyMMdd}.pdf";
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string filePath = Path.Combine(desktopPath, fileName);

                    // Открываем файл через системное приложение
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);

                    StatusMessage = "Документ открыт для просмотра";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при открытии документа: {ex.Message}";
            }
            finally
            {
                ShowStatusMessage = true;
            }
        }

        private void AddDocument()
        {
            // Здесь будет логика добавления нового документа
            // Возможно, открытие диалогового окна для ввода данных
            StatusMessage = "Функция добавления документа в разработке";
            ShowStatusMessage = true;
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