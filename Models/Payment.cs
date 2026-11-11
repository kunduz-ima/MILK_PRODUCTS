using System;
using System.Collections.Generic;

namespace MILK_PRODUCTS.Models;

public partial class Payment
{
    public int Id { get; set; }

    public DateTime? PaymentDate { get; set; } = DateTime.Today;

    public float? PartOfCreditSum { get; set; }

    public float? PercentageSum { get; set; }
    public float? TotalSum { get; set; }
    public int? OverdueDays { get; set; }
    public float? PenaltySum { get; set; }
    public float? Total { get; set; }
    public float? ReamainSum { get; set; }
    public bool? Status { get; set; }
    public int? CreditID { get; set; }
    public virtual Credit? CreditNavigation { get; set; }
}

