﻿using System;
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
        public async Task<List<Attachment>> GetDocumentsAsync(Guid? filterByUserId = null)
        {
            try
            {
                // Начинаем с базового запроса
                var query = _dbContext.Attachments
                    .Include(a => a.Task)
                        .ThenInclude(t => t.OrderPosition)
                            .ThenInclude(op => op.Order)
                    .Include(a => a.UploadedByNavigation)
                    .AsQueryable();

                // Если указан ID пользователя для фильтрации
                if (filterByUserId.HasValue)
                {
                    query = query.Where(a => a.UploadedBy == filterByUserId);
                }

                // Выполняем запрос и возвращаем результаты
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении документов: {ex.Message}");
                return new List<Attachment>();
            }
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
        /// <summary>
        /// Открывает диалог выбора файла для загрузки (с группировкой типов)
        /// </summary>
        public async Task<(string fileName, byte[] content, string fileType)> OpenFilePickerAsync(Window parent)
        {
            var files = await parent.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите документ для загрузки",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
            new FilePickerFileType("Все файлы (*.*)")
            {
                Patterns = new[] { "*.*" }
            },
            new FilePickerFileType("Документы")
            {
                Patterns = new[] { "*.pdf", "*.docx", "*.doc", "*.xlsx", "*.xls", "*.txt", "*.rtf" },
                MimeTypes = new[] {
                    "application/pdf",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/vnd.ms-excel",
                    "text/plain",
                    "application/rtf"
                }
            },
            new FilePickerFileType("Изображения")
            {
                Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.tiff" },
                MimeTypes = new[] { "image/png", "image/jpeg", "image/bmp", "image/gif", "image/tiff" }
            },
            new FilePickerFileType("Чертежи и схемы")
            {
                Patterns = new[] { "*.dwg", "*.dxf" },
                MimeTypes = new[] { "application/acad", "image/vnd.dxf" }
            },
            new FilePickerFileType("Архивы")
            {
                Patterns = new[] { "*.zip", "*.rar", "*.7z" },
                MimeTypes = new[] { "application/zip", "application/x-rar-compressed", "application/x-7z-compressed" }
            }
        }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                // Определяем тип файла по расширению
                string fileType = DetermineFileTypeByExtension(file.Name);

                return (file.Name, ms.ToArray(), fileType);
            }

            return (null, null, null);
        }
        /// <summary>
        /// Определяет MIME-тип файла по расширению
        /// </summary>
        private string DetermineFileTypeByExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                ".tiff" => "image/tiff",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".dwg" => "application/acad",
                ".dxf" => "image/vnd.dxf",
                _ => "application/octet-stream"
            };
        }
        /// <summary>
        /// Открывает диалог сохранения файла
        /// </summary>
        public async Task<string> SaveFilePickerAsync(Window parent, string defaultFileName)
        {
            var extension = Path.GetExtension(defaultFileName).ToLowerInvariant();

            var fileTypeChoices = extension switch
            {
                ".pdf" => new[]
                {
                    new FilePickerFileType("PDF Документ")
                    {
                        Patterns = new[] { "*.pdf" },
                        MimeTypes = new[] { "application/pdf" }
                    }
                },
                        ".docx" => new[]
                        {
                    new FilePickerFileType("Word Документ")
                    {
                        Patterns = new[] { "*.docx" },
                        MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
                    }
                },
                        ".xlsx" => new[]
                        {
                    new FilePickerFileType("Excel Документ")
                    {
                        Patterns = new[] { "*.xlsx" },
                        MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
                    }
                },
                        ".png" => new[]
                        {
                    new FilePickerFileType("PNG Изображение")
                    {
                        Patterns = new[] { "*.png" },
                        MimeTypes = new[] { "image/png" }
                    }
                },
                        ".txt" => new[]
                        {
                    new FilePickerFileType("Текстовый файл")
                    {
                        Patterns = new[] { "*.txt" },
                        MimeTypes = new[] { "text/plain" }
                    }
                },
                        _ => new[]
                        {
                    new FilePickerFileType("Все файлы")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            };

            var file = await parent.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить документ",
                DefaultExtension = extension,
                SuggestedFileName = defaultFileName,
                FileTypeChoices = fileTypeChoices
            });

            return file?.Path.LocalPath;
        }

        /// <summary>
        /// Удаляет документ из базы данных
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(int documentId, bool isAdminOrManager)
        {
            try
            {
                var attachment = await _dbContext.Attachments.FindAsync(documentId);
                if (attachment == null)
                    return false;

                // Проверяем права на удаление - только администратор или менеджер
                if (!isAdminOrManager)
                    return false;

                _dbContext.Attachments.Remove(attachment);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении документа: {ex.Message}");
                return false;
            }
        }
    }
}
