using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class DesignDocumentation
{
    public int Id { get; set; }

    public int? OrderPositionId { get; set; }

    public DateOnly? TechnicalTaskDate { get; set; }

    public DateOnly? ComponentListDate { get; set; }

    public DateOnly? ProductCompositionDate { get; set; }

    public DateOnly? OperationManualDate { get; set; }

    public DateOnly? ManualDate { get; set; }

    public DateOnly? PassportDate { get; set; }

    public DateOnly? DesignDocsDate { get; set; }

    public DateOnly? CuttingWorkshopDate { get; set; }

    public DateOnly? CuttingSubcontractDate { get; set; }

    public DateOnly? MechanicalProcessingDate { get; set; }

    public DateOnly? FurnitureSubcontractDate { get; set; }

    public DateOnly? Printing3dDate { get; set; }

    public DateOnly? ElectronicComponentListDate { get; set; }

    public DateOnly? ElectronicDesignDocsDate { get; set; }

    public DateOnly? ElectronicSoftwareDate { get; set; }

    public DateOnly? PrintingFileDate { get; set; }

    public DateOnly? ArtDesignDate { get; set; }

    public DateOnly? SoftwareDate { get; set; }

    public DateOnly? MaterialsNormsDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual OrderPosition? OrderPosition { get; set; }
}
