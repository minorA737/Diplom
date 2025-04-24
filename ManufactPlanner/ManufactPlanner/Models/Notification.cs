using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class Notification
{
    public int Id { get; set; }

    public Guid? UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? LinkTo { get; set; }

    public string? NotificationType { get; set; }

    public virtual User? User { get; set; }
}
