using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Unit
{
    public short Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Finproduct> Finproducts { get; set; } = new List<Finproduct>();

    public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();
}
