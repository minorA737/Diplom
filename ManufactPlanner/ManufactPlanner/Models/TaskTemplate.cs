using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class TaskTemplate
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    public int? DefaultPriority { get; set; }

    public int? EstimatedDuration { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual TaskCategory? Category { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
