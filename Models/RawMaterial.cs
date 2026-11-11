using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class RawMaterial
{
    public short Id { get; set; }

    public string? Name { get; set; }

    public short? Unit { get; set; }

    public float? CountRawMaterials { get; set; }

    public decimal? Sum { get; set; }

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<PurchaseOfRawMaterial> PurchaseOfRawMaterials { get; set; } = new List<PurchaseOfRawMaterial>();

    public virtual Unit? UnitNavigation { get; set; }
}
