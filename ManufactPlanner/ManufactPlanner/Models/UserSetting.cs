using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class UserSetting
{
    public int Id { get; set; }

    public Guid? UserId { get; set; }

    public bool? NotifyNewTasks { get; set; }

    public bool? NotifyStatusChanges { get; set; }

    public bool? NotifyComments { get; set; }

    public bool? NotifyDeadlines { get; set; }

    public bool? NotifyEmail { get; set; }

    public bool? NotifyDesktop { get; set; }

    public bool? AutoStartEnabled { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? Islighttheme { get; set; }

    public virtual User? User { get; set; }
}
