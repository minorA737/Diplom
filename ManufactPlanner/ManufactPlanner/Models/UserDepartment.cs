using System;
using System.Collections.Generic;

namespace ManufactPlanner.Models;

public partial class UserDepartment
{
    public Guid UserId { get; set; }

    public int DepartmentId { get; set; }

    public bool? IsHead { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
