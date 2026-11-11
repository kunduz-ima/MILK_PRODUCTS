using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MILK_PRODUCTS.Models;

public partial class Credit
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите сумму кредита")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть положительной")]
    public float? CreditSum { get; set; }

    [Required(ErrorMessage = "Укажите дату получения")]
    [DataType(DataType.Date, ErrorMessage = "Некорректный формат даты")]
    public DateTime? DataOfReceive { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Укажите срок кредита")]
    [Range(1, 30, ErrorMessage = "Срок должен быть от 1 до 30 лет")]
    public short? Years { get; set; }

    [Required(ErrorMessage = "Укажите процентную ставку")]
    [Range(0, 100, ErrorMessage = "Процент должен быть от 0 до 100")]
    public float? AnnualPercent { get; set; }

    [Required(ErrorMessage = "Укажите пеню")]
    [Range(0, 100, ErrorMessage = "Пеня должна быть от 0 до 100")]
    public float? Penalty { get; set; }
    public bool? Status { get; set; } = false; // false - активный, true - закрытый
    public DateTime? CreditExpirationDate { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
