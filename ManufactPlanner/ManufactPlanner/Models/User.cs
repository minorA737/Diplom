using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Task> TaskAssignees { get; set; } = new List<Task>();

    public virtual ICollection<Task> TaskCreatedByNavigations { get; set; } = new List<Task>();

    public virtual ICollection<TaskHistory> TaskHistories { get; set; } = new List<TaskHistory>();

    public virtual ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();

    public virtual ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();

    public virtual ICollection<UserDepartment> UserDepartments { get; set; } = new List<UserDepartment>();

    public virtual ICollection<UserSetting> UserSettings { get; set; } = new List<UserSetting>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
