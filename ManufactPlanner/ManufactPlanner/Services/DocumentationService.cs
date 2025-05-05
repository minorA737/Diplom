using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ManufactPlanner.Services
{
    public class DocumentationService
    {
        private readonly PostgresContext _dbContext;

        public DocumentationService(PostgresContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Получает список документов из таблицы Attachment
        /// </summary>
        public async Task<List<Attachment>> GetDocumentsAsync()
        {
            return await _dbContext.Attachments
                .Include(a => a.Task)
                    .ThenInclude(t => t.OrderPosition)
                        .ThenInclude(op => op.Order)
                .Include(a => a.UploadedByNavigation)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Получает документ по ID
        /// </summary>
        public async Task<Attachment> GetDocumentByIdAsync(int id)
        {
            return await _dbContext.Attachments
                .Include(a => a.Task)
                    .ThenInclude(t => t.OrderPosition)
                        .ThenInclude(op => op.Order)
                .Include(a => a.UploadedByNavigation)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        /// <summary>
        /// Загружает файл в базу данных
        /// </summary>
        public async Task<bool> UploadDocumentAsync(string fileName, byte[] fileContent,
            int? orderId, int? orderPositionId, string fileType, Guid userId)
        {
            try
            {
                // Если указан ID позиции заказа, но нам нужно создать задачу для привязки
                int? taskId = null;

                if (orderPositionId.HasValue)
                {
                    // Находим позицию заказа
                    var orderPosition = await _dbContext.OrderPositions
                        .Include(op => op.Tasks)
                        .FirstOrDefaultAsync(op => op.Id == orderPositionId.Value);

                    if (orderPosition != null)
                    {
                        // Если у позиции уже есть задачи, используем первую из них
                        if (orderPosition.Tasks.Any())
                        {
                            taskId = orderPosition.Tasks.First().Id;
                        }
                        else
                        {
                            // Иначе создаем новую задачу для документации
                            var task = new Models.Task
                            {
                                OrderPositionId = orderPositionId.Value,
                                Name = $"Документация для {orderPosition.PositionNumber} - {orderPosition.ProductName}",
                                Description = $"Автоматически созданная задача для хранения документации",
                                Priority = 3, // Низкий приоритет
                                Status = "Документация",
                                CreatedAt = DateTime.Now,
                                CreatedBy = userId
                            };

                            _dbContext.Tasks.Add(task);
                            await _dbContext.SaveChangesAsync();
                            taskId = task.Id;
                        }
                    }
                }

                // Создаем запись о документе
                var attachment = new Attachment
                {
                    FileName = fileName,
                    FileContent = fileContent,
                    TaskId = taskId,
                    FileType = fileType,
                    FileSize = fileContent.Length,
                    UploadedAt = DateTime.Now,
                    UploadedBy = userId
                };

                _dbContext.Attachments.Add(attachment);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке документа: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Сохраняет документ на диск
        /// </summary>
        public async Task<string> SaveDocumentToFileAsync(Attachment document, string filePath)
        {
            if (document?.FileContent == null)
                throw new ArgumentException("Документ не содержит данных");

            await File.WriteAllBytesAsync(filePath, document.FileContent);
            return filePath;
        }

        // Получение списка позиций заказа
        public async Task<List<OrderPosition>> GetOrderPositionsAsync(int orderId)
        {
            return await _dbContext.OrderPositions
                .Where(op => op.OrderId == orderId)
                .Include(op => op.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Открывает диалог выбора файла для загрузки
        /// </summary>
        public async Task<(string fileName, byte[] content, string fileType)> OpenFilePickerAsync(Window parent)
        {
            var files = await parent.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите документ для загрузки",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PDF Документы")
                    {
                        Patterns = new[] { "*.pdf" },
                        MimeTypes = new[] { "application/pdf" }
                    },
                    new FilePickerFileType("Все файлы")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                return (file.Name, ms.ToArray(), "application/pdf");
            }

            return (null, null, null);
        }

        /// <summary>
        /// Открывает диалог сохранения файла
        /// </summary>
        public async Task<string> SaveFilePickerAsync(Window parent, string defaultFileName)
        {
            var file = await parent.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить документ",
                DefaultExtension = ".pdf",
                SuggestedFileName = defaultFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF Документ")
                    {
                        Patterns = new[] { "*.pdf" },
                        MimeTypes = new[] { "application/pdf" }
                    }
                }
            });

            return file?.Path.LocalPath;
        }

        /// <summary>
        /// Удаляет документ из базы данных
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(int id)
        {
            try
            {
                var document = await _dbContext.Attachments.FindAsync(id);
                if (document != null)
                {
                    _dbContext.Attachments.Remove(document);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
