using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class OrderPositionsView
{
    public int? Id { get; set; }

    public string? OrderNumber { get; set; }

    public string? PositionNumber { get; set; }

    public string? ProductName { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? DevelopmentType { get; set; }

    public string? WorkflowTime { get; set; }

    public string? CurrentStatus { get; set; }

    public string? Customer { get; set; }

    public bool? HasInstallation { get; set; }

    public DateOnly? ContractDeadline { get; set; }

    public DateOnly? DeliveryDeadline { get; set; }

    public DateOnly? ShippingDate { get; set; }

    public DateOnly? AcceptanceDate { get; set; }

    public int? ContractQuantity { get; set; }

    public decimal? OrderTotalPrice { get; set; }

    public string? OrderStatus { get; set; }
}
