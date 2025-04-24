using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class Comment
{
    public int Id { get; set; }

    public int? TaskId { get; set; }

    public Guid? UserId { get; set; }

    public string Text { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Task? Task { get; set; }

    public virtual User? User { get; set; }
}
