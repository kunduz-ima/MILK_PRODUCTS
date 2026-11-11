using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Ingredient
{
    public short Id { get; set; }

    public short? Product { get; set; }

    public short? RawMaterials { get; set; }

    public float? CountIngredients { get; set; }

    public virtual Finproduct? ProductNavigation { get; set; }

    public virtual RawMaterial? RawMaterialsNavigation { get; set; }
}
