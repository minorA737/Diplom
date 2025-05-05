using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using ManufactPlanner.ViewModels.Dialogs;
using ManufactPlanner.Views.Dialogs;

namespace ManufactPlanner.ViewModels
{
    public class DocumentationViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly DocumentationService _documentationService;
        private readonly Window _parentWindow;

        private ObservableCollection<DocumentViewModel> _documents;
        private bool _isLoading = false;
        private string _searchText = string.Empty;
        private int _selectedTypeIndex = 0;
        private string _statusMessage = string.Empty;
        private bool _showStatusMessage = false;

        public bool HasDocuments => Documents != null && Documents.Count > 0;

        public ObservableCollection<DocumentViewModel> Documents
        {
            get => _documents;
            set
            {
                this.RaiseAndSetIfChanged(ref _documents, value);
                this.RaisePropertyChanged(nameof(HasDocuments));
            }
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
            set
            {
                this.RaiseAndSetIfChanged(ref _statusMessage, value);
                ShowStatusMessage = !string.IsNullOrEmpty(value);
            }
        }

        public bool ShowStatusMessage
        {
            get => _showStatusMessage;
            set => this.RaiseAndSetIfChanged(ref _showStatusMessage, value);
        }

        public ReactiveCommand<int, Unit> DownloadCommand { get; }
        public ReactiveCommand<int, Unit> ViewCommand { get; }
        public ReactiveCommand<Unit, Unit> AddDocumentCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<int, Unit> DeleteDocumentCommand { get; }

        private ObservableCollection<DocumentViewModel> _allDocuments;



        public DocumentationViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, Window parentWindow)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _parentWindow = parentWindow;
            _documentationService = new DocumentationService(dbContext);

            // Создаем команды
            DownloadCommand = ReactiveCommand.CreateFromTask<int>(DownloadDocumentAsync);
            ViewCommand = ReactiveCommand.CreateFromTask<int>(ViewDocumentAsync);
            AddDocumentCommand = ReactiveCommand.CreateFromTask(AddDocumentAsync);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadDocumentsAsync);
            DeleteDocumentCommand = ReactiveCommand.CreateFromTask<int>(DeleteDocumentAsync);

            // Инициализация коллекции
            Documents = new ObservableCollection<DocumentViewModel>();

            // Асинхронно загружаем документы
            System.Threading.Tasks.Task.Run(() => LoadDocumentsAsync()).ConfigureAwait(false);
        }

        // Конструктор для дизайнера
        public DocumentationViewModel()
        {
            var canExecute = Observable.Return(true);
            DownloadCommand = ReactiveCommand.Create<int>(_ => { }, canExecute);
            ViewCommand = ReactiveCommand.Create<int>(_ => { }, canExecute);
            AddDocumentCommand = ReactiveCommand.Create(() => { }, canExecute);
            RefreshCommand = ReactiveCommand.Create(() => { }, canExecute);
            DeleteDocumentCommand = ReactiveCommand.Create<int>(_ => { }, canExecute);

            // Для отображения в дизайнере
            LoadSampleData();
        }

        private async System.Threading.Tasks.Task LoadDocumentsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка документов...";

                if (_dbContext != null && _documentationService != null)
                {
                    // Загрузка данных из таблицы Attachments с включением информации о задаче, позиции заказа и заказе
                    var attachments = await _documentationService.GetDocumentsAsync();
                    var result = new List<DocumentViewModel>();

                    foreach (var attachment in attachments)
                    {
                        // Получаем связанные данные
                        var orderNumber = attachment.Task?.OrderPosition?.Order?.OrderNumber ?? "Не указан";
                        var productName = attachment.Task?.OrderPosition?.ProductName ?? "Без описания";
                        var userName = attachment.UploadedByNavigation?.FirstName + " " +
                                       attachment.UploadedByNavigation?.LastName ?? "Система";

                        // Определяем тип документа по расширению или MIME-типу
                        string docType = DetermineDocumentType(attachment.FileName, attachment.FileType);

                        result.Add(new DocumentViewModel
                        {
                            Id = attachment.Id,
                            Name = attachment.FileName,
                            Description = productName,
                            Type = docType,
                            OrderNumber = orderNumber,
                            CreatedDate = attachment.UploadedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана",
                            Author = userName,
                            IsAlternate = (result.Count % 2 == 0)
                        });
                    }

                    _allDocuments = new ObservableCollection<DocumentViewModel>(result);
                    Documents = new ObservableCollection<DocumentViewModel>(_allDocuments);

                    if (_allDocuments.Count == 0)
                    {
                        StatusMessage = "Документов не найдено";
                    }
                    else
                    {
                        StatusMessage = $"Загружено документов: {_allDocuments.Count}";
                    }
                }
                else
                {
                    LoadSampleData();
                    StatusMessage = "Работа в режиме демонстрации (без подключения к БД)";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки документов: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        private string DetermineDocumentType(string fileName, string fileType)
        {
            // Определение типа документа по расширению или MIME-типу
            if (fileType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true ||
                fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Анализируем имя файла для определения типа документа
                string lowerFileName = fileName.ToLower();
                if (lowerFileName.Contains("тз") || lowerFileName.Contains("техническое задание") || lowerFileName.Contains("technical"))
                    return "Техническое задание";
                else if (lowerFileName.Contains("спецификац") || lowerFileName.Contains("специф") || lowerFileName.Contains("specification"))
                    return "Спецификация";
                else if (lowerFileName.Contains("руководство") || lowerFileName.Contains("manual") || lowerFileName.Contains("инструкц"))
                    return "Руководство";
                else if (lowerFileName.Contains("чертеж") || lowerFileName.Contains("drawing"))
                    return "Чертеж";
                else if (lowerFileName.Contains("схем") || lowerFileName.Contains("scheme") || lowerFileName.Contains("diagram"))
                    return "Схема";
                else
                    return "PDF документ";
            }

            return "Документ";
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
                    d.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    d.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
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
            StatusMessage = Documents.Count > 0
                ? $"Найдено документов: {Documents.Count}"
                : "Документов не найдено";
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
                StatusMessage = "Подготовка документа к скачиванию...";

                // Получаем документ из базы
                var attachment = await _documentationService.GetDocumentByIdAsync(documentId);
                if (attachment == null || attachment.FileContent == null)
                {
                    StatusMessage = "Ошибка: Документ не найден или не содержит данных";
                    return;
                }

                // Открываем диалог выбора места сохранения
                string defaultFileName = attachment.FileName ?? $"document_{documentId}.pdf";
                string filePath = await _documentationService.SaveFilePickerAsync(_parentWindow, defaultFileName);

                if (string.IsNullOrEmpty(filePath))
                {
                    StatusMessage = "Скачивание отменено";
                    return;
                }

                // Сохраняем файл
                await _documentationService.SaveDocumentToFileAsync(attachment, filePath);

                StatusMessage = $"Документ успешно сохранен: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при скачивании документа: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task ViewDocumentAsync(int documentId)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Подготовка документа для просмотра...";

                // Получаем документ из базы
                var attachment = await _documentationService.GetDocumentByIdAsync(documentId);
                if (attachment == null || attachment.FileContent == null)
                {
                    StatusMessage = "Ошибка: Документ не найден или не содержит данных";
                    return;
                }

                // Сохраняем во временный файл и открываем его
                string tempPath = Path.Combine(Path.GetTempPath(), attachment.FileName ?? $"document_{documentId}.pdf");
                await _documentationService.SaveDocumentToFileAsync(attachment, tempPath);

                // Открываем файл через системную программу просмотра PDF
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                StatusMessage = "Документ открыт для просмотра";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при открытии документа: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Новое поле для хранения выбранного заказа
        private OrderListItem _selectedOrder;
        private int? _selectedOrderPositionId;

        // Модифицируйте метод AddDocumentAsync
        private async System.Threading.Tasks.Task AddDocumentAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Выберите документ для загрузки...";

                if (_parentWindow == null)
                {
                    StatusMessage = "Ошибка: не удалось открыть диалог выбора файла";
                    return;
                }

                // Открываем диалог выбора файла
                var (fileName, content, fileType) = await _documentationService.OpenFilePickerAsync(_parentWindow);

                if (content == null || string.IsNullOrEmpty(fileName))
                {
                    StatusMessage = "Загрузка отменена";
                    return;
                }

                // Открываем диалог выбора заказа
                var orderDialog = new OrderSelectionDialog(_dbContext);
                await orderDialog.ShowDialog(_parentWindow);

                var dialogViewModel = orderDialog.DataContext as OrderSelectionDialogViewModel;
                if (dialogViewModel != null && dialogViewModel.DialogResult && dialogViewModel.SelectedOrder != null)
                {
                    _selectedOrder = dialogViewModel.SelectedOrder;

                    // Если заказ выбран, предлагаем выбрать позицию заказа
                    var orderPositionId = await SelectOrderPositionAsync(_selectedOrder.Id);
                    _selectedOrderPositionId = orderPositionId;
                }
                else
                {
                    StatusMessage = "Выбор заказа отменен";
                    return;
                }

                // Получаем текущего пользователя из MainWindowViewModel
                var currentUserId = _mainWindowViewModel.CurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    // Используем значение по умолчанию, если ID не определен
                    currentUserId = new Guid("00000000-0000-0000-0000-000000000001");
                }

                // Загружаем файл в базу данных с привязкой к заказу
                bool success = await _documentationService.UploadDocumentAsync(
                    fileName,
                    content,
                    _selectedOrder.Id,
                    _selectedOrderPositionId,
                    fileType,
                    currentUserId);

                if (success)
                {
                    StatusMessage = $"Документ '{fileName}' успешно загружен для заказа {_selectedOrder.OrderNumber}";
                    // Обновляем список документов
                    await LoadDocumentsAsync();
                }
                else
                {
                    StatusMessage = "Ошибка при загрузке документа";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при загрузке документа: {ex.Message}";
                Console.WriteLine($"Подробности исключения: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Метод для выбора позиции заказа
        private async Task<int?> SelectOrderPositionAsync(int orderId)
        {
            try
            {
                // Получаем позиции заказа
                var orderPositions = await _documentationService.GetOrderPositionsAsync(orderId);

                if (orderPositions == null || orderPositions.Count == 0)
                {
                    // Заказ не имеет позиций
                    return null;
                }

                // Создаем и показываем диалог выбора позиции заказа
                var positionDialog = new OrderPositionSelectionDialog(orderPositions);
                await positionDialog.ShowDialog(_parentWindow);

                var positionViewModel = positionDialog.DataContext as OrderPositionSelectionDialogViewModel;
                if (positionViewModel != null && positionViewModel.DialogResult && positionViewModel.SelectedOrderPosition != null)
                {
                    return positionViewModel.SelectedOrderPosition.Id;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выборе позиции заказа: {ex.Message}");
                return null;
            }
        }

        private async System.Threading.Tasks.Task DeleteDocumentAsync(int documentId)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Удаление документа...";

                bool success = await _documentationService.DeleteDocumentAsync(documentId);

                if (success)
                {
                    StatusMessage = "Документ успешно удален";
                    // Обновляем список документов
                    await LoadDocumentsAsync();
                }
                else
                {
                    StatusMessage = "Ошибка при удалении документа";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при удалении документа: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
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