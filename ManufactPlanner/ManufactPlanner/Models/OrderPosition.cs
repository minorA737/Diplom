using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class OrderPosition
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public string PositionNumber { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal? Price { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? DevelopmentType { get; set; }

    public string? WorkflowTime { get; set; }

    public string? CurrentStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<DesignDocumentation> DesignDocumentations { get; set; } = new List<DesignDocumentation>();

    public virtual ICollection<MaterialsManagement> MaterialsManagements { get; set; } = new List<MaterialsManagement>();

    public virtual Order? Order { get; set; }

    public virtual ICollection<ProductionDetail> ProductionDetails { get; set; } = new List<ProductionDetail>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
