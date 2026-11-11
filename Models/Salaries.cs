using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Salaries
{
    public int Id { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public int? ParticipationPurchase { get; set; }

    public int? ParticipationProduction { get; set; }

    public int? ParticipationSale { get; set; }
    public decimal? Bonus { get; set; } // Бонус
    public decimal? TotalAmount { get; set; } // Общая зарплата
    public string? Status { get; set; } // Статус (выдано или нет)
    public int? EmployeeID { get; set; }
    public virtual Employee? EmployeeNavigation { get; set; }
}