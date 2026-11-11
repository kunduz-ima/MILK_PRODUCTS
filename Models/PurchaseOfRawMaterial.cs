using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class PurchaseOfRawMaterial
{
    public short Id { get; set; }

    public short? RawMaterials { get; set; }

    public float? CountPurchase { get; set; }

    public decimal? Sum { get; set; }

    public DateTime? Date { get; set; }

    public int? Employee { get; set; }

    public virtual Employee? EmployeeNavigation { get; set; }

    public virtual RawMaterial? RawMaterialsNavigation { get; set; }
}
