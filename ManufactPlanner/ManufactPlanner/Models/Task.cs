using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class Task
{
    public int Id { get; set; }

    public int? OrderPositionId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int Priority { get; set; }

    public string? Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public Guid? AssigneeId { get; set; }

    public string? CoAssignees { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public string? Stage { get; set; }

    public string? DebuggingStatus { get; set; }

    public string? Notes { get; set; }

    public virtual User? Assignee { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual OrderPosition? OrderPosition { get; set; }

    public virtual ICollection<TaskDevelopmentDetail> TaskDevelopmentDetails { get; set; } = new List<TaskDevelopmentDetail>();

    public virtual ICollection<TaskHistory> TaskHistories { get; set; } = new List<TaskHistory>();

    public virtual ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
}
