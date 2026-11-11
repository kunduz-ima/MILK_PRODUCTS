using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MILK_PRODUCTS.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;


namespace MILK_PRODUCTS.Controllers
{
    [Authorize(Roles = "Administrator, Accountant, Director")]
    public class CreditsController : BaseController
    {
        private readonly DbmsContext _context;
        private readonly IConfiguration _configuration;

        public CreditsController(DbmsContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Credits (уже должен быть)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Credits.ToListAsync());
        }

        // GET: Credits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var credit = await _context.Credits
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (credit == null) return NotFound();

            return View(credit);
        }

        // POST: Credits/Export
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Export(int creditId, string exportType)
        {
            var credit = _context.Credits
                .Include(c => c.Payments)
                .FirstOrDefault(c => c.Id == creditId);

            if (credit == null) return NotFound();

            return exportType.ToLower() switch
            {
                "excel" => ExportToExcel(credit),
                "word" => ExportToWord(credit),
                "pdf" => ExportToPdf(credit),
                _ => RedirectToAction(nameof(Details), new { id = creditId })
            };
        }
        private IActionResult ExportToExcel(Credit credit)
        {
            using var workbook = new XLWorkbook();

            // Лист 1: информация о кредите
            var creditSheet = workbook.Worksheets.Add("Credit Info");
            creditSheet.Cell(1, 1).Value = "Credit Report";
            creditSheet.Cell(1, 1).Style.Font.Bold = true;
            creditSheet.Cell(1, 1).Style.Font.FontSize = 14;

            creditSheet.Cell(3, 1).Value = "Credit ID:";
            creditSheet.Cell(3, 2).Value = credit.Id;
            creditSheet.Cell(4, 1).Value = "Credit Sum:";
            creditSheet.Cell(4, 2).Value = credit.CreditSum;
            creditSheet.Cell(5, 1).Value = "Start Date:";
            creditSheet.Cell(5, 2).Value = credit.DataOfReceive?.ToShortDateString();
            creditSheet.Cell(6, 1).Value = "Term (years):";
            creditSheet.Cell(6, 2).Value = credit.Years;
            creditSheet.Cell(7, 1).Value = "Interest Rate:";
            creditSheet.Cell(7, 2).Value = credit.AnnualPercent + "%";
            creditSheet.Cell(8, 1).Value = "Penalty Rate:";
            creditSheet.Cell(8, 2).Value = credit.Penalty + "%";
            creditSheet.Cell(9, 1).Value = "Status:";
            creditSheet.Cell(9, 2).Value = credit.Status == true ? "Closed" : "Active";
            creditSheet.Cell(10, 1).Value = "Expiration Date:";
            creditSheet.Cell(10, 2).Value = credit.CreditExpirationDate?.ToShortDateString();

            // Лист 2: платежи
            var paymentsSheet = workbook.Worksheets.Add("Payments");
            paymentsSheet.Cell(1, 1).Value = "Date";
            paymentsSheet.Cell(1, 2).Value = "Amount";

            var payments = credit.Payments.ToList();
            for (int i = 0; i < payments.Count; i++)
            {
                paymentsSheet.Cell(i + 2, 1).Value = payments[i].PaymentDate?.ToShortDateString(); // Исправлено
                paymentsSheet.Cell(i + 2, 2).Value = payments[i].Total; // Исправлено (используем Total)
            }

            // Итог по платежам
            if (payments.Any())
            {
                int totalRow = payments.Count + 2;
                paymentsSheet.Cell(totalRow, 1).Value = "Total:";
                paymentsSheet.Cell(totalRow, 2).Value = payments.Sum(p => p.Total); // Исправлено
                paymentsSheet.Cell(totalRow, 2).Style.Font.Bold = true;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CreditReport_{credit.Id}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ======================== ЭКСПОРТ В WORD ========================
        private IActionResult ExportToWord(Credit credit)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new Text("Credit Report"))));

                // Информация о кредите
                body.AppendChild(new Paragraph(new Run(new Text($"Credit ID: {credit.Id}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Credit Sum: {credit.CreditSum}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Start Date: {credit.DataOfReceive?.ToShortDateString()}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Term (years): {credit.Years}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Interest Rate: {credit.AnnualPercent}%"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Penalty Rate: {credit.Penalty}%"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Status: {(credit.Status == true ? "Closed" : "Active")}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Expiration Date: {credit.CreditExpirationDate?.ToShortDateString()}"))));

                body.AppendChild(new Paragraph(new Run(new Text("")))); // пустая строка

                // Таблица платежей
                if (credit.Payments.Any())
                {
                    body.AppendChild(new Paragraph(new Run(new Text("Payments:"))));
                    var table = new Table();
                    var tableProps = new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    );
                    table.AppendChild(tableProps);

                    // Заголовок таблицы
                    var headerRow = new TableRow();
                    headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Date")))));
                    headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Amount")))));
                    table.AppendChild(headerRow);

                    foreach (var payment in credit.Payments)
                    {
                        var row = new TableRow();
                        row.AppendChild(new TableCell(new Paragraph(new Run(new Text(payment.PaymentDate?.ToShortDateString() ?? "")))));
                        row.AppendChild(new TableCell(new Paragraph(new Run(new Text(payment.Total?.ToString("F2") ?? "0")))));
                        table.AppendChild(row);
                    }

                    // Итог
                    var totalRow = new TableRow();
                    totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Total:")))));
                    totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text(credit.Payments.Sum(p => p.Total)?.ToString("F2") ?? "0")))));
                    table.AppendChild(totalRow);

                    body.AppendChild(table);
                }
                else
                {
                    body.AppendChild(new Paragraph(new Run(new Text("No payments yet."))));
                }

                // Подписи
                body.AppendChild(new Paragraph(new Run(new Text(""))));
                body.AppendChild(new Paragraph(new Run(new Text("Director: _______________ (Kunduz Imanalieva)"))));
                body.AppendChild(new Paragraph(new Run(new Text("Administrator: _______________ (Ayana Kasymova)"))));
            }

            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"CreditReport_{credit.Id}_{DateTime.Now:yyyyMMdd}.docx");
        }

        // ======================== ЭКСПОРТ В PDF ========================
        private IActionResult ExportToPdf(Credit credit)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Content().Column(col =>
                    {
                        // Заголовок
                        col.Item().AlignCenter().Text("Credit Report")
                            .FontFamily("Times New Roman").Bold().FontSize(16);

                        col.Item().PaddingVertical(10);

                        // Информация о кредите
                        col.Item().Text($"Credit ID: {credit.Id}").FontFamily("Times New Roman").FontSize(12);
                        col.Item().Text($"Credit Sum: {credit.CreditSum}").FontFamily("Times New Roman").FontSize(12);
                        col.Item().Text($"Start Date: {credit.DataOfReceive?.ToShortDateString()}").FontFamily("Times New Roman").FontSize(12);
                        col.Item().Text($"Term (years): {credit.Years}").FontFamily("Times New Roman").FontSize(12);
                        col.Item().Text($"Interest Rate: {credit.AnnualPercent}%").FontFamily("Times New Roman").FontSize(12);
                        col.Item().Text($"Penalty Rate: {credit.Penalty}%").FontFamily("Times New Roman").FontSize(12);
                        col.Item().Text($"Status: {(credit.Status == true ? "Closed" : "Active")}").FontFamily("Times New Roman").FontSize(12);
                        col.Item().Text($"Expiration Date: {credit.CreditExpirationDate?.ToShortDateString()}").FontFamily("Times New Roman").FontSize(12);

                        col.Item().PaddingVertical(10);

                        // Заголовок таблицы платежей
                        col.Item().Text("Payments:").FontFamily("Times New Roman").Bold().FontSize(14);

                        if (credit.Payments.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();
                                    cols.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Date").FontFamily("Times New Roman").Bold();
                                    header.Cell().Text("Amount").FontFamily("Times New Roman").Bold();
                                });

                                float total = 0;
                                foreach (var payment in credit.Payments)
                                {
                                    table.Cell().Text(payment.PaymentDate?.ToShortDateString() ?? "").FontFamily("Times New Roman");
                                    table.Cell().Text(payment.Total?.ToString("F2") ?? "0").FontFamily("Times New Roman");
                                    total += payment.Total ?? 0;
                                }

                                // Итог
                                table.Cell().Text("Total:").FontFamily("Times New Roman").Bold();
                                table.Cell().Text(total.ToString("F2")).FontFamily("Times New Roman").Bold();
                            });
                        }
                        else
                        {
                            col.Item().Text("No payments yet.").FontFamily("Times New Roman").Italic();
                        }

                        // Подписи
                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(signCol =>
                            {
                                signCol.Item().Text("Director: __________________").FontFamily("Times New Roman").FontSize(12);
                                signCol.Item().PaddingTop(5).Text("Kunduz Imanalieva").FontFamily("Times New Roman").FontSize(12);
                            });
                            row.RelativeItem().AlignCenter().Column(signCol =>
                            {
                                signCol.Item().Text("Administrator: __________________").FontFamily("Times New Roman").FontSize(12);
                                signCol.Item().PaddingTop(5).Text("Ayana Kasymova").FontFamily("Times New Roman").FontSize(12);
                            });
                        });
                    });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", $"CreditReport_{credit.Id}_{DateTime.Now:yyyyMMdd}.pdf");
        }


        // GET: Credits/Create
        public IActionResult Create()
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var model = new Credit
            {
                DataOfReceive = DateTime.Today // Устанавливаем сегодняшнюю дату
            };
            return View(model);
        }

        // POST: Credits/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Credit credit)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (ModelState.IsValid)
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand("CreateCredit", connection);
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    // Добавляем параметры для хранимой процедуры
                    command.Parameters.AddWithValue("@CreditSum", credit.CreditSum);
                    command.Parameters.AddWithValue("@DataOfReceive", credit.DataOfReceive);
                    command.Parameters.AddWithValue("@Years", credit.Years);
                    command.Parameters.AddWithValue("@AnnualPercent", credit.AnnualPercent);
                    command.Parameters.AddWithValue("@Penalty", credit.Penalty);

                    // Возвращаемое значение (0 = успех, 1-99 = ошибки)
                    var returnParameter = command.Parameters.Add("@ReturnVal", System.Data.SqlDbType.Int);
                    returnParameter.Direction = System.Data.ParameterDirection.ReturnValue;

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        var result = (int)returnParameter.Value;

                        switch (result)
                        {
                            case 0: // Успех
                                TempData["SuccessMessage"] = "Кредит успешно создан";
                                return RedirectToAction("Index");
                            case 1: // Ошибка: некорректная сумма
                                ModelState.AddModelError("CreditSum", "Некорректная сумма кредита");
                                break;
                            case 2: // Ошибка: некорректный срок
                                ModelState.AddModelError("Years", "Некорректный срок кредита (1-30 лет)");
                                break;
                            case 3: // Ошибка: некорректный процент
                                ModelState.AddModelError("AnnualPercent", "Процент должен быть от 0 до 100");
                                break;
                            case 4: // Ошибка: уже есть активный кредит
                                ModelState.AddModelError("", "Уже есть активный кредит");
                                break;
                            case 99: // Другая ошибка БД
                                ModelState.AddModelError("", "Ошибка при создании кредита");
                                break;
                            default:
                                ModelState.AddModelError("", "Неизвестная ошибка");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Ошибка базы данных: {ex.Message}");
                    }
                }
            }
            return View(credit);
        }

        private bool CreditExists(int id)
        {
            return _context.Credits.Any(e => e.Id == id);
        }
    }
}
