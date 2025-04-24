using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class TaskDetailsView
{
    public int? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int? Priority { get; set; }

    public string? Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Stage { get; set; }

    public string? DebuggingStatus { get; set; }

    public string? Notes { get; set; }

    public string? AssigneeName { get; set; }

    public string? CoAssignees { get; set; }

    public string? PositionNumber { get; set; }

    public string? ProductName { get; set; }

    public string? OrderNumber { get; set; }

    public string? Customer { get; set; }

    public string? SchematicStatus { get; set; }

    public string? MountingSchemeStatus { get; set; }

    public string? FirmwareVersion { get; set; }
}
