using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string? Fullname { get; set; }

    public short? Position { get; set; }

    public decimal? Salary { get; set; }

    public string? Address { get; set; }

    public string? Telephone { get; set; }

    public virtual Position? PositionNavigation { get; set; }

    public virtual ICollection<Production> Productions { get; set; } = new List<Production>();

    public virtual ICollection<PurchaseOfRawMaterial> PurchaseOfRawMaterials { get; set; } = new List<PurchaseOfRawMaterial>();

    public virtual ICollection<SaleOfProduct> SaleOfProducts { get; set; } = new List<SaleOfProduct>();
    public virtual ICollection<Salaries> Salaries { get; set; } = new List<Salaries>();
}
