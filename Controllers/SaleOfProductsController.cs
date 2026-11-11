using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    [Authorize(Roles = "Administrator, Director, Manager")]
    public class SaleOfProductsController : BaseController
    {
        private readonly DbmsContext _context;

        public SaleOfProductsController(DbmsContext context)
        {
            _context = context;
        }

        // GET: SaleOfProducts с фильтрацией по датам
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.SaleOfProducts
                .Include(s => s.EmployeeNavigation)
                .Include(s => s.ProductNavigation)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.Date <= endDate.Value);

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(await query.ToListAsync());
        }

        // POST: Экспорт
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Export(string startDate, string endDate, string exportType)
        {
            DateTime? start = string.IsNullOrEmpty(startDate) ? null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? null : DateTime.Parse(endDate);

            var query = _context.SaleOfProducts
                .Include(s => s.ProductNavigation)
                .Include(s => s.EmployeeNavigation)
                .AsQueryable();

            if (start.HasValue) query = query.Where(s => s.Date >= start.Value);
            if (end.HasValue) query = query.Where(s => s.Date <= end.Value);

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
        private IActionResult ExportToExcel(List<SaleOfProduct> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sales Report");

            // Заголовки
            worksheet.Cell(1, 1).Value = "№";
            worksheet.Cell(1, 2).Value = "Product";
            worksheet.Cell(1, 3).Value = "Quantity";
            worksheet.Cell(1, 4).Value = "Sum";
            worksheet.Cell(1, 5).Value = "Date";
            worksheet.Cell(1, 6).Value = "Employee";

            // Данные
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                worksheet.Cell(i + 2, 1).Value = i + 1;
                worksheet.Cell(i + 2, 2).Value = item.ProductNavigation?.Name ?? "";
                worksheet.Cell(i + 2, 3).Value = item.CountSaleOfProducts;
                worksheet.Cell(i + 2, 4).Value = item.Sum;
                worksheet.Cell(i + 2, 5).Value = item.Date?.ToShortDateString() ?? "";
                worksheet.Cell(i + 2, 6).Value = item.EmployeeNavigation?.Fullname ?? "";
            }

            // Итоги
            int totalRow = data.Count + 2;
            worksheet.Cell(totalRow, 2).Value = "Total:";
            worksheet.Cell(totalRow, 3).Value = data.Sum(x => x.CountSaleOfProducts);
            worksheet.Cell(totalRow, 4).Value = data.Sum(x => x.Sum);
            worksheet.Cell(totalRow, 3).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 4).Style.Font.Bold = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalesReport_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ======================== ЭКСПОРТ В WORD ========================
        private IActionResult ExportToWord(List<SaleOfProduct> data)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Стили (аналогично Production)
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

                var signatureStyle = new Style
                {
                    Type = StyleValues.Paragraph,
                    StyleId = "Signature",
                    StyleName = new StyleName { Val = "Signature" }
                };
                signatureStyle.Append(new StyleParagraphProperties(new Justification { Val = JustificationValues.Left }));
                styles.Append(signatureStyle);

                stylesPart.Styles = styles;

                // Заголовок
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new Text("Sales Report"))));

                // Период
                if (data.Any())
                {
                    var minDate = data.Min(x => x.Date);
                    var maxDate = data.Max(x => x.Date);
                    body.AppendChild(new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                        new Run(new Text($"From {minDate:MM/dd/yyyy} to {maxDate:MM/dd/yyyy}"))));
                }

                // Дата генерации
                body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new Text($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}"))));

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
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Product")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Quantity")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Sum")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Date")))));
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("Employee")))));
                table.AppendChild(headerRow);

                float totalQuantity = 0;
                decimal totalSum = 0;
                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    var row = new TableRow();
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text((i + 1).ToString())))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.ProductNavigation?.Name ?? "")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.CountSaleOfProducts.ToString())))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.Sum?.ToString("F2") ?? "0")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.Date?.ToShortDateString() ?? "")))));
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(item.EmployeeNavigation?.Fullname ?? "")))));
                    table.AppendChild(row);
                    totalQuantity += item.CountSaleOfProducts ?? 0;
                    totalSum += item.Sum ?? 0;
                }

                // Итоговая строка
                var totalRow = new TableRow();
                var totalCell = new TableCell(new Paragraph(new Run(new Text("Total:"))));
                totalCell.TableCellProperties = new TableCellProperties { GridSpan = new GridSpan { Val = 2 } };
                totalRow.AppendChild(totalCell);
                totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text(totalQuantity.ToString())))));
                totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text(totalSum.ToString("F2"))))));
                totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("")))));
                totalRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("")))));
                table.AppendChild(totalRow);

                body.AppendChild(table);

                // Подписи
                body.AppendChild(new Paragraph(new Run(new Text(""))));
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
                $"SalesReport_{DateTime.Now:yyyyMMdd}.docx");
        }

        // ======================== ЭКСПОРТ В PDF ========================
        private IActionResult ExportToPdf(List<SaleOfProduct> data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var startDate = data.Any() ? data.Min(x => x.Date)?.ToString("MM/dd/yyyy") ?? "N/A" : "N/A";
            var endDate = data.Any() ? data.Max(x => x.Date)?.ToString("MM/dd/yyyy") ?? "N/A" : "N/A";
            var generatedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm");
            var totalQuantity = data.Sum(x => x.CountSaleOfProducts) ?? 0;
            var totalSum = data.Sum(x => x.Sum) ?? 0;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Content().Column(col =>
                    {
                        // Заголовок
                        col.Item().AlignCenter().Text("Sales Report")
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
                            });

                            table.Header(header =>
                            {
                                header.Cell().AlignCenter().Text("№").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Product").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Quantity").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Sum").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Date").FontFamily("Times New Roman").Bold().FontSize(14);
                                header.Cell().Text("Employee").FontFamily("Times New Roman").Bold().FontSize(14);
                            });

                            for (int i = 0; i < data.Count; i++)
                            {
                                var item = data[i];
                                table.Cell().AlignCenter().Text((i + 1).ToString()).FontFamily("Times New Roman");
                                table.Cell().Text(item.ProductNavigation?.Name ?? "").FontFamily("Times New Roman");
                                table.Cell().Text(item.CountSaleOfProducts?.ToString() ?? "").FontFamily("Times New Roman");
                                table.Cell().Text(item.Sum?.ToString("F2") ?? "0").FontFamily("Times New Roman");
                                table.Cell().Text(item.Date?.ToShortDateString() ?? "").FontFamily("Times New Roman");
                                table.Cell().Text(item.EmployeeNavigation?.Fullname ?? "").FontFamily("Times New Roman");
                            }

                            // Итоги
                            table.Cell().ColumnSpan(2).AlignLeft().Text(" Total:").FontFamily("Times New Roman").Bold();
                            table.Cell().Text(totalQuantity.ToString()).FontFamily("Times New Roman").Bold();
                            table.Cell().Text(totalSum.ToString("F2")).FontFamily("Times New Roman").Bold();
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
            return File(stream, "application/pdf", $"SalesReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // GET: SaleOfProducts/Create
        public IActionResult Create()
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            var model = new SaleOfProduct
            {
                Date = DateTime.Today // Устанавливаем сегодняшнюю дату
            };
            ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname");
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name");
            return View(model);
        }

        // POST: SaleOfProducts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Product,CountSaleOfProducts,Sum,Date,Employee")] SaleOfProduct saleOfProduct)
        {
            var check = CheckEditPermission();
            if (check != null) return check;
            if (ModelState.IsValid)
            {
                var product = await _context.Finproducts.FindAsync(saleOfProduct.Product);

                if (saleOfProduct.CountSaleOfProducts > product.CountProducts)
                {
                    ModelState.AddModelError("", "Недостаточно продукта на складе для продажи.");
                    return View(saleOfProduct);
                }

                var budget = await _context.Budgets.FirstOrDefaultAsync();
                if (budget != null)
                {
                    decimal salePercentage = (decimal)(budget.SalePercentage ?? 0);
                    decimal sum = product.Sum ?? 0;
                    decimal countProducts = (decimal)(product.CountProducts ?? 0);

                    // Расчет цены за единицу
                    decimal unitPrice = sum / countProducts;

                    // Расчет цены за единицу с учетом процента
                    decimal unitPriceWithPercentage = unitPrice * (1 + (salePercentage / 100));

                    // Общая цена продажи
                    decimal totalSalePrice = unitPriceWithPercentage * (decimal)(saleOfProduct.CountSaleOfProducts ?? 0);

                    budget.BudgetAmount += totalSalePrice;

                    product.CountProducts -= (float)(saleOfProduct.CountSaleOfProducts ?? 0);
                    product.Sum -= unitPrice * (decimal)(saleOfProduct.CountSaleOfProducts ?? 0);

                    _context.Budgets.Update(budget);
                    _context.Finproducts.Update(product);

                    saleOfProduct.Sum = totalSalePrice;
                }

                _context.Add(saleOfProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

                ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname", saleOfProduct.Employee);
                ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", saleOfProduct.Product);
                return View(saleOfProduct);
            
        }




















        // GET: SaleOfProducts/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saleOfProduct = await _context.SaleOfProducts.FindAsync(id);
            if (saleOfProduct == null)
            {
                return NotFound();
            }
            ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname", saleOfProduct.Employee);
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", saleOfProduct.Product);
            return View(saleOfProduct);
        }

        // POST: SaleOfProducts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("Id,Product,CountSaleOfProducts,Sum,Date,Employee")] SaleOfProduct saleOfProduct)
        {
            if (id != saleOfProduct.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(saleOfProduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SaleOfProductExists(saleOfProduct.Id))
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
            ViewData["Employee"] = new SelectList(_context.Employees, "Id", "Fullname", saleOfProduct.Employee);
            ViewData["Product"] = new SelectList(_context.Finproducts, "Id", "Name", saleOfProduct.Product);
            return View(saleOfProduct);
        }

        // GET: SaleOfProducts/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saleOfProduct = await _context.SaleOfProducts
                .Include(s => s.EmployeeNavigation)
                .Include(s => s.ProductNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (saleOfProduct == null)
            {
                return NotFound();
            }

            return View(saleOfProduct);
        }

        // POST: SaleOfProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            var saleOfProduct = await _context.SaleOfProducts.FindAsync(id);
            if (saleOfProduct != null)
            {
                _context.SaleOfProducts.Remove(saleOfProduct);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SaleOfProductExists(short id)
        {
            return _context.SaleOfProducts.Any(e => e.Id == id);
        }
    }
}
