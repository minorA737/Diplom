using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Customer { get; set; } = null!;

    public bool? HasInstallation { get; set; }

    public DateOnly? ContractDeadline { get; set; }

    public DateOnly? DeliveryDeadline { get; set; }

    public DateOnly? ShippingDate { get; set; }

    public DateOnly? AcceptanceDate { get; set; }

    public int ContractQuantity { get; set; }

    public decimal? TotalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public string? Status { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<OrderPosition> OrderPositions { get; set; } = new List<OrderPosition>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
