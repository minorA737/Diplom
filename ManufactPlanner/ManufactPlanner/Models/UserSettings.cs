// Models/UserSettings.cs
using System;

namespace ManufactPlanner.Models
{
    public class UserSettings
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public bool NotifyNewTasks { get; set; } = true;
        public bool NotifyTaskStatusChanges { get; set; } = true;
        public bool NotifyComments { get; set; } = true;
        public bool NotifyDeadlines { get; set; } = true;
        public bool NotifyEmail { get; set; } = true;
        public bool NotifyDesktop { get; set; } = true;
        public bool AutoStartEnabled { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }

        public virtual User User { get; set; }
    }
}