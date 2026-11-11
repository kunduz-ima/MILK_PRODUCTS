using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Finproduct
{
    public short Id { get; set; }

    public string? Name { get; set; }

    public short? Unit { get; set; }

    public float? CountProducts { get; set; }

    public decimal? Sum { get; set; }

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<Production> Productions { get; set; } = new List<Production>();

    public virtual ICollection<SaleOfProduct> SaleOfProducts { get; set; } = new List<SaleOfProduct>();

    public virtual Unit? UnitNavigation { get; set; }
}
