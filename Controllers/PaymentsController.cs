using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MILK_PRODUCTS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace MILK_PRODUCTS.Controllers
{
    [Authorize(Roles = "Administrator, Accountant, Director")]
    public class PaymentsController : BaseController
    {
        private readonly DbmsContext _context;
        private readonly IConfiguration _configuration;

        public PaymentsController(DbmsContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Payments
        // Главная страница с выбором кредита
        public IActionResult Index()
        {
            var activeCredits = _context.Credits
                .Where(c => c.Status == false)
                .ToList();

            return View(activeCredits);
        }

        // Просмотр платежей по кредиту
        [HttpGet]
        public IActionResult Payments(int creditId)
        {
            var credit = _context.Credits.FirstOrDefault(c => c.Id == creditId);
            if (credit == null)
            {
                return NotFound();
            }

            var payments = _context.Payments
                .Where(p => p.CreditID == creditId)
                .OrderBy(p => p.PaymentDate)
                .ToList();

            ViewBag.CreditId = creditId;
            ViewBag.CreditInfo = $"Кредит №{creditId}";

            return View(payments);
        }

        // GET: Payments/Create
        // Форма создания платежа
        [HttpGet]
        public IActionResult Create(int creditId)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var credit = _context.Credits.FirstOrDefault(c => c.Id == creditId);
            if (credit == null)
            {
                return NotFound();
            }

            ViewBag.CreditId = creditId;
            return View(new Payment { PaymentDate = DateTime.Today });
        }

        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // Обработка создания платежа
        [HttpPost]
        public IActionResult Create(int creditId, DateTime paymentDate)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("CreatePayment", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@PaymentDate", paymentDate);
                command.Parameters.AddWithValue("@CreditID", creditId);

                var returnParameter = command.Parameters.Add("@ReturnVal", System.Data.SqlDbType.Int);
                returnParameter.Direction = System.Data.ParameterDirection.ReturnValue;

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    var result = (int)returnParameter.Value;

                    switch (result)
                    {
                        case 0:
                            TempData["SuccessMessage"] = "Платёж успешно создан";
                            return RedirectToAction("Payments", new { creditId });
                        case 1:
                            TempData["ErrorMessage"] = "Не найден активный кредит";
                            break;
                        case 2:
                            TempData["ErrorMessage"] = "Дата платежа раньше даты получения кредита";
                            break;
                        case 3:
                            TempData["SuccessMessage"] = "Платёж успешно создан. Кредит полностью погашен!";
                            return RedirectToAction("Payments", new { creditId });
                        case 4:
                            TempData["ErrorMessage"] = "Недостаточно средств в бюджете";
                            break;
                        case 5:
                            TempData["ErrorMessage"] = "Пропущен предыдущий месяц платежа";
                            break;
                        case 6:
                            TempData["ErrorMessage"] = "Платёж за этот месяц уже существует";
                            break;
                        case 99:
                            TempData["ErrorMessage"] = "Ошибка при создании платежа";
                            break;
                        default:
                            TempData["ErrorMessage"] = "Неизвестная ошибка";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Ошибка базы данных: {ex.Message}";
                }
            }

            return RedirectToAction("Create", new { creditId });
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }
    }
}
