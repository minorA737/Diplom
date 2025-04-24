using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class TaskHistory
{
    public int Id { get; set; }

    public int? TaskId { get; set; }

    public Guid? ChangedBy { get; set; }

    public DateTime? ChangedAt { get; set; }

    public string FieldName { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public virtual User? ChangedByNavigation { get; set; }

    public virtual Task? Task { get; set; }
}
