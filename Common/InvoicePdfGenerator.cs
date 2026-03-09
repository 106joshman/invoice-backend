using iText.Kernel.Pdf;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.IO.Font.Constants;
using iText.IO.Image;

using InvoiceService.Models;

public class InvoicePdfGenerator
{
    private static readonly Color ColorDark = new DeviceRgb(26, 26, 46);
    private static readonly Color ColorGray = new DeviceRgb(120, 120, 120);
    private static readonly Color ColorLightGray = new DeviceRgb(245, 245, 245);
    private static readonly Color ColorLine = new DeviceRgb(224, 224, 224);

    public static byte[] Generate(Invoice invoice)
    {
        using var stream = new MemoryStream();

        var writer = new PdfWriter(stream);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf, PageSize.A4);

        document.SetMargins(25, 25, 25, 25);

        var FontNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var FontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        // Null safe fields
        var businessName = invoice.Business?.Name ?? "";
        var businessEmail = invoice.Business?.Email ?? "";
        var businessLogo = invoice.Business?.CompanyLogoUrl ?? "";

        var customerName = invoice.Customer?.Name ?? "";
        var customerCompany = invoice.Customer?.Company ?? "";
        var customerPhone = invoice.Customer?.PhoneNumber ?? "";
        var customerAddress = invoice.Customer?.Address ?? "";

        var bankName = invoice.Business?.PaymentInfo?.BankName ?? "";
        var accountName = invoice.Business?.PaymentInfo?.AccountName ?? "";
        var accountNumber = invoice.Business?.PaymentInfo?.AccountNumber ?? "";

        var invoiceNumber = invoice.InvoiceNumber ?? "";
        var notes = invoice.Notes ?? "";

        string status = invoice.Status?.ToString() ?? "PENDING";

        Color statusColor = status switch
        {
            "PAID" => new DeviceRgb(46, 125, 50),
            "PENDING" => new DeviceRgb(237, 108, 2),
            "OVERDUE" => new DeviceRgb(211, 47, 47),
            _ => ColorGray
        };

        // HEADER
        var header = new Table(new float[] { 1f, 3f, 2f })
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER);

        var logoCell = new Cell().SetBorder(Border.NO_BORDER);

        if (!string.IsNullOrWhiteSpace(businessLogo))
        {
            try
            {
                var image = ImageDataFactory.Create(businessLogo);
                var logo = new Image(image).ScaleToFit(60, 60);
                logoCell.Add(logo);
            }
            catch { }
        }

        header.AddCell(logoCell);

        var businessCell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .Add(new Paragraph(businessName)
                .SetFont(FontBold)
                .SetFontSize(14)
                .SetFontColor(ColorDark))
            .Add(new Paragraph(businessEmail)
                .SetFontSize(9)
                .SetFontColor(ColorGray));

        header.AddCell(businessCell);

        var metaCell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph(status)
                .SetFont(FontBold)
                .SetFontSize(10)
                .SetFontColor(statusColor))
            .Add(BuildMetaRow("Invoice No:", invoiceNumber, FontNormal, FontBold))
            .Add(BuildMetaRow("Issue Date:", invoice.IssueDate.ToString("MMM dd, yyyy"), FontNormal, FontBold))
            .Add(BuildMetaRow("Due Date:", invoice.DueDate.ToString("MMM dd, yyyy"), FontNormal, FontBold));

        header.AddCell(metaCell);

        document.Add(header);

        document.Add(new LineSeparator(new SolidLine())
            .SetStrokeColor(ColorLine)
            .SetMarginTop(8)
            .SetMarginBottom(10));

        // BILL TO + PAYMENT INFO
        var infoTable = new Table(new float[] { 1f, 1f })
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER);

        var billCell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .Add(new Paragraph("BILL TO")
                .SetFont(FontBold)
                .SetFontSize(9))
            .Add(new Paragraph(customerName)
                .SetFont(FontBold)
                .SetFontSize(10))
            .Add(new Paragraph(customerCompany).SetFontSize(9))
            .Add(new Paragraph(customerPhone).SetFontSize(9))
            .Add(new Paragraph(customerAddress).SetFontSize(9));

        var paymentCell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph("PAYMENT INFORMATION")
                .SetFont(FontBold)
                .SetFontSize(9))
            .Add(new Paragraph(bankName)
                .SetFont(FontBold)
                .SetFontSize(10))
            .Add(new Paragraph(accountName).SetFontSize(9))
            .Add(new Paragraph(accountNumber).SetFontSize(9));

        infoTable.AddCell(billCell);
        infoTable.AddCell(paymentCell);

        document.Add(infoTable);

        document.Add(new Paragraph("\n"));

        // ITEMS TABLE
        var table = new Table(new float[] { 1f, 4f, 1f, 2f, 2f })
            .UseAllAvailableWidth();

        string[] headers = { "#", "Description", "Qty", "Unit Price", "Total" };

        foreach (var h in headers)
        {
            table.AddHeaderCell(new Cell()
                .SetBackgroundColor(ColorLightGray)
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph(h)
                    .SetFont(FontBold)
                    .SetFontSize(9)));
        }

        int index = 1;

        foreach (var item in invoice.Items)
        {
            decimal total = item.Quantity * item.UnitPrice;

            table.AddCell(ItemCell(index.ToString(), FontNormal));
            table.AddCell(ItemCell(item.Description ?? "", FontNormal));
            table.AddCell(ItemCell(item.Quantity.ToString(), FontNormal));
            table.AddCell(ItemCell(item.UnitPrice.ToString("N2"), FontNormal));

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .Add(new Paragraph($"NGN {total:N2}")
                    .SetFont(FontNormal)));

            index++;
        }

        document.Add(table);

        // TOTALS
        // var totals = new Table(new float[] { 3f, 2f })
        //     .UseAllAvailableWidth()
        //     .SetBorder(Border.NO_BORDER)
        //     .SetMarginTop(10);

        // AddTotalRow(totals, "Subtotal", invoice.Subtotal, FontNormal);
        // AddTotalRow(totals, "Discount", -invoice.Discount, FontNormal);
        // AddTotalRow(totals, $"Tax ({invoice.TaxRate * 100:0.#}%)", invoice.TaxAmount, FontNormal);
        // AddTotalRow(totals, "Total", invoice.Total, FontBold);

        // document.Add(totals);
        // TOTALS SECTION
        var totalsWrapper = new Table(new float[] { 3f, 1.5f })
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMarginTop(10);

        // Empty left side
        totalsWrapper.AddCell(new Cell().SetBorder(Border.NO_BORDER));

        // Right totals container
        var totalsCell = new Cell().SetBorder(Border.NO_BORDER);

        var totals = new Table(2)
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER);

        AddTotalRow(totals, "Subtotal", invoice.Subtotal, FontNormal);
        AddTotalRow(totals, "Discount", -invoice.Discount, FontNormal);
        AddTotalRow(totals, $"Tax ({invoice.TaxRate * 100:0.#}%)", invoice.TaxAmount, FontNormal);
        AddTotalRow(totals, "Total", invoice.Total, FontBold);

        totalsCell.Add(totals);

        totalsWrapper.AddCell(totalsCell);

        document.Add(totalsWrapper);

        document.Add(new Paragraph("\n\n"));

        // FOOTER
        // var footer = new Table(new float[] { 2f, 1f })
        //     .UseAllAvailableWidth()
        //     .SetBorder(Border.NO_BORDER);

        // footer.AddCell(new Cell()
        //     .SetBorder(Border.NO_BORDER)
        //     .Add(new Paragraph("NOTES")
        //         .SetFont(FontBold)
        //         .SetFontSize(9))
        //     .Add(new Paragraph(string.IsNullOrWhiteSpace(notes)
        //         ? "Thank you for your business."
        //         : notes)
        //         .SetFontSize(9)));

        // footer.AddCell(new Cell()
        //     .SetBorder(Border.NO_BORDER)
        //     .SetTextAlignment(TextAlignment.RIGHT)
        //     .Add(new Paragraph("Have a Question?")
        //         .SetFont(FontBold)
        //         .SetFontSize(9))
        //     .Add(new Paragraph(businessEmail)
        //         .SetFontSize(9)));

        // document.Add(footer);
        // FOOTER
        var footer = new Table(new float[] { 2f, 1f })
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER);

        footer.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .Add(new Paragraph("NOTES")
                .SetFont(FontBold)
                .SetFontSize(9))
            .Add(new Paragraph(string.IsNullOrWhiteSpace(notes)
                ? "Thank you for your business."
                : notes)
                .SetFontSize(9)));

        footer.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph("Have a Question?")
                .SetFont(FontBold)
                .SetFontSize(9))
            .Add(new Paragraph(businessEmail)
                .SetFontSize(9)));

        // lock footer at bottom
        footer.SetFixedPosition(
            1,     // page number
            25,    // X position
            20,    // Y position
            PageSize.A4.GetWidth() - 50
        );

        document.Add(footer);

        document.Close();

        return stream.ToArray();
    }

    private static Paragraph BuildMetaRow(string label, string value, PdfFont normal, PdfFont bold)
    {
        return new Paragraph()
            .Add(new Text(label + " ").SetFont(normal).SetFontSize(9))
            .Add(new Text(value).SetFont(bold).SetFontSize(9));
    }

    private static Cell ItemCell(string text, PdfFont font)
    {
        return new Cell()
            .SetBorder(Border.NO_BORDER)
            .Add(new Paragraph(text)
                .SetFont(font)
                .SetFontSize(9));
    }

    private static void AddTotalRow(Table table, string label, decimal value, PdfFont font)
    {
        table.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph(label).SetFont(font)));

        table.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph($"NGN {value:N2}")
                .SetFont(font)));
    }
}