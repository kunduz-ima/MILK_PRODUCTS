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
using System.Linq;
using System.Threading.Tasks;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;


namespace MILK_PRODUCTS.Controllers
{
    [Authorize(Roles = "Administrator, Accountant, Director")]
    public class SalariesController : Controller
    {
        private readonly DbmsContext _context;

        public SalariesController(DbmsContext context)
        {
            _context = context;
        }

        // GET: Salaries
        public IActionResult Index(int? year, int? month)
        {
            if (!year.HasValue || !month.HasValue)
            {
                year = DateTime.Now.Year;
                month = DateTime.Now.Month;
            }

            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 5, 10).ToList();
            ViewBag.Months = Enumerable.Range(1, 12).ToList();
            ViewBag.MonthNames = new string[] { "", "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                                      "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };

            var salaries = _context.Salaries
                .Include(s => s.EmployeeNavigation)
                .Where(s => s.Year == year && s.Month == month)
                .ToList();

            ViewBag.HasData = salaries.Any();
            ViewBag.TotalAmount = salaries.Sum(s => s.TotalAmount) ?? 0;

            return View(salaries);
        }


        // POST: Salaries/Export
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Export(int year, int month, string exportType)
        {
            var salaries = _context.Salaries
                .Include(s => s.EmployeeNavigation)
                .Where(s => s.Year == year && s.Month == month)
                .ToList();

            if (!salaries.Any())
            {
                // Если нет данных, можно вернуть сообщение или просто редирект
                TempData["ErrorMessage"] = "Нет данных за выбранный период";
                return RedirectToAction(nameof(Index), new { year, month });
            }

            return exportType.ToLower() switch
            {
                "excel" => ExportToExcel(salaries, year, month),
                "word" => ExportToWord(salaries, year, month),
                "pdf" => ExportToPdf(salaries, year, month),
                _ => RedirectToAction(nameof(Index), new { year, month })
            };
        }

        // ======================== ЭКСПОРТ В EXCEL ========================
        private IActionResult ExportToExcel(List<Salaries> salaries, int year, int month)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Salaries {month}/{year}");

            // Заголовки
            worksheet.Cell(1, 1).Value = "№";
            worksheet.Cell(1, 2).Value = "Employee";
            worksheet.Cell(1, 3).Value = "Base Salary";
            worksheet.Cell(1, 4).Value = "Participation Production";
            worksheet.Cell(1, 5).Value = "Participation Purchase";
            worksheet.Cell(1, 6).Value = "Participation Sale";
            worksheet.Cell(1, 7).Value = "Bonus";
            worksheet.Cell(1, 8).Value = "Total";
            worksheet.Cell(1, 9).Value = "Status";

            // Данные
            for (int i = 0; i < salaries.Count; i++)
            {
                var item = salaries[i];
                worksheet.Cell(i + 2, 1).Value = i + 1;
                worksheet.Cell(i + 2, 2).Value = item.EmployeeNavigation?.Fullname ?? "";
                worksheet.Cell(i + 2, 3).Value = item.EmployeeNavigation?.Salary;
                worksheet.Cell(i + 2, 4).Value = item.ParticipationProduction;
                worksheet.Cell(i + 2, 5).Value = item.ParticipationPurchase;
                worksheet.Cell(i + 2, 6).Value = item.ParticipationSale;
                worksheet.Cell(i + 2, 7).Value = item.Bonus;
                worksheet.Cell(i + 2, 8).Value = item.TotalAmount;
                worksheet.Cell(i + 2, 9).Value = item.Status;
            }

            // Итог
            int totalRow = salaries.Count + 2;
            worksheet.Cell(totalRow, 2).Value = "Total:";
            worksheet.Cell(totalRow, 8).Value = salaries.Sum(s => s.TotalAmount);
            worksheet.Cell(totalRow, 8).Style.Font.Bold = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Salaries_{year}_{month:D2}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ======================== ЭКСПОРТ В WORD ========================
        private IActionResult ExportToWord(List<Salaries> salaries, int year, int month)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Стили
                var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
                Styles styles = new Styles();

                var headingStyle = new Style
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "Heading1",
                    StyleName = new StyleName { Val = "Heading 1" }
                };
                headingStyle.Append(new StyleParagraphProperties(new Justification { Val = JustificationValues.Center }));
                styles.Append(headingStyle);

                stylesPart.Styles = styles;

                // Заголовок
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new Text($"Salaries Report for {month}/{year}"))));

                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // Таблица
                var table = new Table();
                var tableProps = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 4 },
                        new BottomBorder { Val = BorderValues.Single, Size = 4 },
                        new LeftBorder { Val = BorderValues.Single, Size = 4 },
                        new RightBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                    ),
                    new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
                );
                table.AppendChild(tableProps);

                // Заголовок таблицы
                var headerRow = new TableRow();
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("№")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Employee")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Base Salary")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Prod.")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Purch.")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Sale")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Bonus")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Total")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Status")))));
                table.AppendChild(headerRow);

                decimal totalAmount = 0;
                for (int i = 0; i < salaries.Count; i++)
                {
                    var item = salaries[i];
                    var row = new TableRow();
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text((i + 1).ToString())))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.EmployeeNavigation?.Fullname ?? "")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.EmployeeNavigation?.Salary?.ToString("F2") ?? "0")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.ParticipationProduction?.ToString() ?? "0")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.ParticipationPurchase?.ToString() ?? "0")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.ParticipationSale?.ToString() ?? "0")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.Bonus?.ToString("F2") ?? "0")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.TotalAmount?.ToString("F2") ?? "0")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.Status ?? "")))));
                    table.AppendChild(row);
                    totalAmount += item.TotalAmount ?? 0;
                }

                // Итоговая строка
                var totalRow = new TableRow();
                var totalCell = new TableCell(new Paragraph(new Run(new Text("Total:"))));
                totalCell.TableCellProperties = new TableCellProperties { GridSpan = new GridSpan { Val = 7 } };
                totalRow.AppendChild(totalCell);
                totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text(totalAmount.ToString("F2"))))));
                totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text(""))))); // пустая для статуса
                table.AppendChild(totalRow);

                body.AppendChild(table);

                // Дата генерации
                body.AppendChild(new Paragraph(new Run(new Text(""))));
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                    new Run(new Text($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}"))));

                // Подписи
                body.AppendChild(new Paragraph(new Run(new Text(""))));
                body.AppendChild(new Paragraph(new Run(new Text("Director: _______________ (Kunduz Imanalieva)"))));
                body.AppendChild(new Paragraph(new Run(new Text("Administrator: _______________ (Ayana Kasymova)"))));
            }

            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"Salaries_{year}_{month:D2}_{DateTime.Now:yyyyMMdd}.docx");
        }

        // ======================== ЭКСПОРТ В PDF ========================
        private IActionResult ExportToPdf(List<Salaries> salaries, int year, int month)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var totalAmount = salaries.Sum(s => s.TotalAmount) ?? 0;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Content().Column(col =>
                    {
                        // Заголовок
                        col.Item().AlignCenter().Text($"Salaries Report for {month}/{year}")
                            .FontFamily("Times New Roman").Bold().FontSize(16);

                        col.Item().PaddingVertical(10);

                        // Таблица
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(30);
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().AlignCenter().Text("№").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Employee").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Base Salary").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Prod.").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Purch.").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Sale").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Bonus").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Total").FontFamily("Times New Roman").Bold().FontSize(10);
                                header.Cell().Text("Status").FontFamily("Times New Roman").Bold().FontSize(10);
                            });

                            for (int i = 0; i < salaries.Count; i++)
                            {
                                var item = salaries[i];
                                table.Cell().AlignCenter().Text((i + 1).ToString()).FontFamily("Times New Roman");
                                table.Cell().Text(item.EmployeeNavigation?.Fullname ?? "").FontFamily("Times New Roman");
                                table.Cell().Text(item.EmployeeNavigation?.Salary?.ToString("F2") ?? "0").FontFamily("Times New Roman");
                                table.Cell().Text(item.ParticipationProduction?.ToString() ?? "0").FontFamily("Times New Roman");
                                table.Cell().Text(item.ParticipationPurchase?.ToString() ?? "0").FontFamily("Times New Roman");
                                table.Cell().Text(item.ParticipationSale?.ToString() ?? "0").FontFamily("Times New Roman");
                                table.Cell().Text(item.Bonus?.ToString("F2") ?? "0").FontFamily("Times New Roman");
                                table.Cell().Text(item.TotalAmount?.ToString("F2") ?? "0").FontFamily("Times New Roman");
                                table.Cell().Text(item.Status ?? "").FontFamily("Times New Roman");
                            }

                            // Итог
                            table.Cell().ColumnSpan(7).AlignLeft().Text(" Total:").FontFamily("Times New Roman").Bold();
                            table.Cell().Text(totalAmount.ToString("F2")).FontFamily("Times New Roman").Bold();
                            table.Cell().Text(""); // пустая для статуса
                        });

                        // Дата генерации
                        col.Item().PaddingTop(20).AlignRight().Text($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}")
                            .FontFamily("Times New Roman").FontSize(10);

                        // Подписи
                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(signCol =>
                            {
                                signCol.Item().Text("Director: __________________")
                                    .FontFamily("Times New Roman").FontSize(12);
                                signCol.Item().PaddingTop(5).Text("Kunduz Imanalieva")
                                    .FontFamily("Times New Roman").FontSize(12);
                            });
                            row.RelativeItem().AlignCenter().Column(signCol =>
                            {
                                signCol.Item().Text("Administrator: __________________")
                                    .FontFamily("Times New Roman").FontSize(12);
                                signCol.Item().PaddingTop(5).Text("Ayana Kasymova")
                                    .FontFamily("Times New Roman").FontSize(12);
                            });
                        });
                    });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", $"Salaries_{year}_{month:D2}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // GET: Salaries/Generate
        public IActionResult Generate(int year, int month)
        {
            if (!_context.Employees.Any())
            {
                TempData["ErrorMessage"] = "Нет сотрудников для генерации зарплат.";
                return RedirectToAction(nameof(Index), new { year, month });
            }

            try
            {
                _context.Database.ExecuteSqlRaw("EXEC [dbo].[CheckBudgetAndGenerateSalaries] @Year, @Month",
                    new SqlParameter("@Year", year),
                    new SqlParameter("@Month", month));

                TempData["SuccessMessage"] = "Зарплаты успешно сгенерированы.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при генерации зарплат: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { year, month });
        }


        // GET: Salaries/Pay
        public IActionResult Pay(int year, int month)
        {
            try
            {
                _context.Database.ExecuteSqlRaw("EXEC [dbo].[CheckBudgetAndPaySalary] @Year, @Month",
                    new SqlParameter("@Year", year),
                    new SqlParameter("@Month", month));

                TempData["SuccessMessage"] = "Зарплаты успешно выплачены.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при выплате зарплат: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { year, month });
        }

        // GET: Salaries/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = _context.Salaries
                .Include(s => s.EmployeeNavigation)
                .FirstOrDefault(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.Status == "Выдано")
            {
                TempData["ErrorMessage"] = "Нельзя редактировать уже выданные зарплаты.";
                return RedirectToAction(nameof(Index), new { year = salary.Year, month = salary.Month });
            }

            return View(salary);
        }

        // POST: Salaries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("Id,TotalAmount")] Salaries salary)
        {
            if (id != salary.Id)
            {
                return NotFound();
            }

            var existingSalary = _context.Salaries.Find(id);
            if (existingSalary == null)
            {
                return NotFound();
            }

            if (existingSalary.Status == "Выдано")
            {
                TempData["ErrorMessage"] = "Нельзя редактировать уже выданные зарплаты.";
                return RedirectToAction(nameof(Index), new { year = existingSalary.Year, month = existingSalary.Month });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Обновляем только TotalAmount
                    existingSalary.TotalAmount = salary.TotalAmount;
                    _context.Update(existingSalary);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Зарплата успешно обновлена.";
                    return RedirectToAction(nameof(Index), new { year = existingSalary.Year, month = existingSalary.Month });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalaryExists(salary.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Если что-то пошло не так, загружаем связанные данные для отображения формы
            _context.Entry(existingSalary).Reference(s => s.EmployeeNavigation).Load();
            return View(existingSalary);
        }

        private bool SalaryExists(int id)
        {
            return _context.Salaries.Any(e => e.Id == id);
        }

        private bool SalariesExists(int id)
        {
            return _context.Salaries.Any(e => e.Id == id);
        }
    }
}
