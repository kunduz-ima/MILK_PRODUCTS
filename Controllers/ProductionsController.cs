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
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace MILK_PRODUCTS.Controllers
{
    [Authorize(Roles = "Administrator, Director, Technologist")]
    public class ProductionsController : BaseController
    {
        private readonly DbmsContext _context;

        public ProductionsController(DbmsContext context)
        {
            _context = context;
        }

        // GET: Productions
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Productions
                .Include(p => p.ProductNavigation)   // навигационное свойство (Finproduct)
                .Include(p => p.EmployeeNavigation)  // Employee
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.Date <= endDate.Value);

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(await query.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Export(string startDate, string endDate, string exportType)
        {
            DateTime? start = string.IsNullOrEmpty(startDate) ? null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? null : DateTime.Parse(endDate);

            var query = _context.Productions
                .Include(p => p.ProductNavigation)
                .Include(p => p.EmployeeNavigation)
                .AsQueryable();

            if (start.HasValue) query = query.Where(p => p.Date >= start.Value);
            if (end.HasValue) query = query.Where(p => p.Date <= end.Value);

            var data = query.ToList();

            return exportType.ToLower() switch
            {
                "excel" => ExportToExcel(data),
                "word" => ExportToWord(data),
                "pdf" => ExportToPdf(data),
                _ => RedirectToAction(nameof(Index))
            };
        }

        // ======================== ЭКСПОРТ В EXCEL ========================
        private IActionResult ExportToExcel(List<Production> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Production Report");

            // Заголовки
            worksheet.Cell(1, 1).Value = "№";
            worksheet.Cell(1, 2).Value = "Product";
            worksheet.Cell(1, 3).Value = "Quantity";
            worksheet.Cell(1, 4).Value = "Date";
            worksheet.Cell(1, 5).Value = "Employee";

            // Данные
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                worksheet.Cell(i + 2, 1).Value = i + 1;
                worksheet.Cell(i + 2, 2).Value = item.ProductNavigation?.Name ?? "";
                worksheet.Cell(i + 2, 3).Value = item.CountProduction;
                worksheet.Cell(i + 2, 4).Value = item.Date?.ToShortDateString() ?? "";
                worksheet.Cell(i + 2, 5).Value = item.EmployeeNavigation?.Fullname ?? "";
            }

            // Итоговая строка (сумма количества)
            var totalRow = data.Count + 2;
            worksheet.Cell(totalRow, 2).Value = "Total:";
            worksheet.Cell(totalRow, 3).Value = data.Sum(x => x.CountProduction);
            worksheet.Cell(totalRow, 3).Style.Font.Bold = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ProductionReport_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ======================== ЭКСПОРТ В WORD ========================
        private IActionResult ExportToWord(List<Production> data)
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

                // Стиль заголовка (по центру)
                var headingStyle = new Style
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "Heading1",
                    StyleName = new StyleName { Val = "Heading 1" }
                };
                headingStyle.Append(new StyleParagraphProperties(new Justification { Val = JustificationValues.Center }));
                styles.Append(headingStyle);

                // Стиль для подписи (по левому краю)
                var signatureStyle = new Style
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "Signature",
                    StyleName = new StyleName { Val = "Signature" }
                };
                signatureStyle.Append(new StyleParagraphProperties(new Justification { Val = JustificationValues.Left }));
                styles.Append(signatureStyle);

                stylesPart.Styles = styles;

                // Заголовок отчета
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new Text("Production Report"))));

                // Период
                var minDate = data.Min(x => x.Date);
                var maxDate = data.Max(x => x.Date);
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new Text($"From {minDate:MM/dd/yyyy} to {maxDate:MM/dd/yyyy}"))));

                // Дата генерации
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new Text($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}"))));

                body.AppendChild(new Paragraph(new Run(new Text("")))); // пустая строка

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
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Product")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Quantity")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Date")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Employee")))));
                table.AppendChild(headerRow);

                float totalQuantity = 0;
                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    var row = new TableRow();
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text((i + 1).ToString())))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.ProductNavigation?.Name ?? "")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.CountProduction.ToString())))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.Date?.ToShortDateString() ?? "")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.EmployeeNavigation?.Fullname ?? "")))));
                    table.AppendChild(row);
                    totalQuantity += item.CountProduction ?? 0;
                }

                // Строка итогов
                var totalRowTable = new TableRow();
                var totalCell = new TableCell(new Paragraph(new Run(new Text("Total:"))));
                totalCell.TableCellProperties = new TableCellProperties { GridSpan = new GridSpan { Val = 2 } };
                totalRowTable.AppendChild(totalCell);
                totalRowTable.AppendChild(new TableCell(new Paragraph(new Run(new Text(totalQuantity.ToString())))));
                totalRowTable.AppendChild(new TableCell(new Paragraph(new Run(new Text("")))));
                totalRowTable.AppendChild(new TableCell(new Paragraph(new Run(new Text("")))));
                table.AppendChild(totalRowTable);

                body.AppendChild(table);

                // Пустая строка перед подписями
                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // Подписи
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                    new Run(new Text("Director: _______________ (Kunduz Imanalieva)"))));
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                    new Run(new Text("Administrator: _______________ (Ayana Kasymova)"))));
            }

            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"ProductionReport_{DateTime.Now:yyyyMMdd}.docx");
        }

        // ======================== ЭКСПОРТ В PDF ========================
        private IActionResult ExportToPdf(List<Production> data)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var startDate = data.Min(x => x.Date)?.ToString("MM/dd/yyyy") ?? "N/A";
            var endDate = data.Max(x => x.Date)?.ToString("MM/dd/yyyy") ?? "N/A";
            var generatedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm");
            var totalQuantity = data.Sum(x => x.CountProduction) ?? 0;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Content().Column(col =>
                    {
                        // Заголовок
                        col.Item().AlignCenter().Text("Production Report")
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
                            });

                            table.Header(header =>
                            {
                                header.Cell().AlignCenter().Text("№").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Product").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Quantity").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Date").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Employee").FontFamily("Times New Roman").Bold().FontSize(14);
                            });

                            for (int i = 0; i < data.Count; i++)
                            {
                                var item = data[i];
                                table.Cell().AlignCenter().Text((i + 1).ToString()).FontFamily("Times New Roman");
                                table.Cell().Text(item.ProductNavigation?.Name ?? "").FontFamily("Times New Roman");
                                table.Cell().Text(item.CountProduction?.ToString() ?? "").FontFamily("Times New Roman");
                                table.Cell().Text(item.Date?.ToShortDateString() ?? "").FontFamily("Times New Roman");
                                table.Cell().Text(item.EmployeeNavigation?.Fullname ?? "").FontFamily("Times New Roman");
                            }

                            // Итоговая строка
                            table.Cell().ColumnSpan(2).AlignLeft().Text(" Total:").FontFamily("Times New Roman").Bold();
                            table.Cell().Text(totalQuantity.ToString()).FontFamily("Times New Roman").Bold();
                            table.Cell().ColumnSpan(2).Text("");
                        });

                        // Период
                        col.Item().PaddingTop(20).AlignCenter().Text($"From {startDate} to {endDate}")
                            .FontFamily("Times New Roman").FontSize(12);

                        // Дата генерации
                        col.Item().AlignCenter().Text($"Generated: {generatedDate}")
                            .FontFamily("Times New Roman").FontSize(12);

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
            return File(stream, "application/pdf", $"ProductionReport_{DateTime.Now:yyyyMMdd}.pdf");
        }


        public async Task<bool> CheckRawMaterialsAsync(int productId, double quantity)
        {
            var statusParam = new SqlParameter("@status", SqlDbType.Bit) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync("EXEC [dbo].[CheckRawMaterials] @ProductID, @Quantity, @status OUT",
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@Quantity", quantity),
                statusParam);

            return (bool)statusParam.Value; // Возвращаем статус наличия сырья
        }

        public async Task ProduceProductAsync(int productId, double quantity, DateTime date, int employeeId)
        {
            await _context.Database.ExecuteSqlRawAsync("EXEC [dbo].[ProduceProduct] @ProductID, @Quantity, @Date, @EmployeeID",
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@Quantity", quantity),
                new SqlParameter("@Date", date),
                new SqlParameter("@EmployeeID", employeeId));
        }

        // GET: Productions/Create
        public IActionResult Create()
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var model = new Production
            {
                Date = DateTime.Today // Устанавливаем сегодняшнюю дату
            };
            ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname");
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name");
            return View(model);
        }

        // POST: Productions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Product,CountProduction,Date,Employee")] Production production)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (ModelState.IsValid)
            {
                bool hasEnoughMaterials = await CheckRawMaterialsAsync(production.Product.Value, production.CountProduction.Value);

                if (!hasEnoughMaterials)
                {
                    ModelState.AddModelError("", "Недостаточно сырья для производства.");
                    ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname", production.Employee);
                    ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", production.Product);
                    return View(production);
                }

                await ProduceProductAsync(production.Product.Value, (float)production.CountProduction, production.Date.Value, production.Employee.Value);
                return RedirectToAction(nameof(Index));
            }
            ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname", production.Employee);
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", production.Product);
            return View(production);
        }

        // GET: Productions/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id == null)
            {
                return NotFound();
            }

            var production = await _context.Productions.FindAsync(id);
            if (production == null)
            {
                return NotFound();
            }
            ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname", production.Employee);
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", production.Product);
            return View(production);
        }

        // POST: Productions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("Id,Product,CountProduction,Date,Employee")] Production production)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id != production.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(production);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductionExists(production.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname", production.Employee);
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", production.Product);
            return View(production);
        }

        // GET: Productions/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (id == null)
            {
                return NotFound();
            }

            var production = await _context.Productions
                .Include(p => p.EmployeeNavigation)
                .Include(p => p.ProductNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (production == null)
            {
                return NotFound();
            }

            return View(production);
        }

        // POST: Productions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var production = await _context.Productions.FindAsync(id);
            if (production != null)
            {
                _context.Productions.Remove(production);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductionExists(short id)
        {
            return _context.Productions.Any(e => e.Id == id);
        }
    }
}
