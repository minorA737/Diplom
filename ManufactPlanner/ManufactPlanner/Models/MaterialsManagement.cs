using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class MaterialsManagement
{
    public int Id { get; set; }

    public int? OrderPositionId { get; set; }

    public DateOnly? MaterialSelectionDeadline { get; set; }

    public DateOnly? MaterialCompletionDeadline { get; set; }

    public DateOnly? MaterialOrderDate { get; set; }

    public DateOnly? MaterialProvisionDate { get; set; }

    public DateOnly? ReoOrderDate { get; set; }

    public DateOnly? ReoProvisionDate { get; set; }

    public DateOnly? CuttingOrderDate { get; set; }

    public DateOnly? CuttingProvisionDate { get; set; }

    public DateOnly? TkiOrderDate { get; set; }

    public DateOnly? TkiDeliveryDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Notes { get; set; }

    public virtual OrderPosition? OrderPosition { get; set; }
}
