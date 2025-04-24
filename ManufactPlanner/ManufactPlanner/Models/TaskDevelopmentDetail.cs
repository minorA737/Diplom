using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class TaskDevelopmentDetail
{
    public int Id { get; set; }

    public int? TaskId { get; set; }

    public string? SchematicStatus { get; set; }

    public string? MountingSchemeStatus { get; set; }

    public string? FirmwareVersion { get; set; }

    public string? DebuggingNotes { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Task? Task { get; set; }
}
