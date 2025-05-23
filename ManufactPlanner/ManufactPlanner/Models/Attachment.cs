﻿using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class Attachment
{
    public int Id { get; set; }

    public int? TaskId { get; set; }

    public string FileName { get; set; } = null!;

    public string? FilePath { get; set; }

    public string? FileType { get; set; }

    public long? FileSize { get; set; }

    public DateTime? UploadedAt { get; set; }

    public Guid? UploadedBy { get; set; }

    /// <summary>
    /// Содержимое файла в бинарном формате (PDF, документы и пр.)
    /// </summary>
    public byte[]? FileContent { get; set; }

    public virtual Task? Task { get; set; }

    public virtual User? UploadedByNavigation { get; set; }
}
