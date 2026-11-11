using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Budget
{
    public int Id { get; set; }

    public decimal? BudgetAmount { get; set; }

    public float? SalePercentage { get; set; }

    public float? BonusPercentage { get; set; }
}
