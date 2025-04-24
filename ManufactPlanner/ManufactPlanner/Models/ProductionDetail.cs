using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class ProductionDetail
{
    public int Id { get; set; }

    public int? OrderPositionId { get; set; }

    public string? OrderNumber { get; set; }

    public string? MasterName { get; set; }

    public DateOnly? ProductionDate { get; set; }

    public DateOnly? DebuggingDate { get; set; }

    public DateOnly? AcceptanceDate { get; set; }

    public DateOnly? PackagingDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Notes { get; set; }

    public virtual OrderPosition? OrderPosition { get; set; }
}
