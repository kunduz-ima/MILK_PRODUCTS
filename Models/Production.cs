using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Production
{
    public short Id { get; set; }

    public short? Product { get; set; }

    public float? CountProduction { get; set; }

    public DateTime? Date { get; set; }

    public int? Employee { get; set; }

    public virtual Employee? EmployeeNavigation { get; set; }

    public virtual Finproduct? ProductNavigation { get; set; }
}
