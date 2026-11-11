using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Position
{
    public short Id { get; set; }

    public string? Position1 { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
