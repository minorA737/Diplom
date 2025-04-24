using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class TaskUser
{
    public int TaskId { get; set; }

    public Guid UserId { get; set; }

    public string? Role { get; set; }

    public virtual Task Task { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
