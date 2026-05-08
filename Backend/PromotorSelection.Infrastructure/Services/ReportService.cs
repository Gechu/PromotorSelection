using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Infrastructure.Services;

public class ReportService : IReportService
{
    public byte[] GenerateExcelReport(AdminReportDto data)
    {
        using var workbook = new XLWorkbook();

        var ws1 = workbook.Worksheets.Add("Wszyscy Studenci");
        ws1.Cell(1, 1).Value = "Student";
        ws1.Cell(1, 2).Value = "Nr Albumu";
        ws1.Cell(1, 3).Value = "Średnia";
        ws1.Cell(1, 4).Value = "Przypisany Promotor";
        ws1.Range("A1:D1").Style.Font.Bold = true;

        for (int i = 0; i < data.AllAssignments.Count; i++)
        {
            var item = data.AllAssignments[i];
            ws1.Cell(i + 2, 1).Value = item.StudentName;
            ws1.Cell(i + 2, 2).Value = item.AlbumNumber;
            ws1.Cell(i + 2, 3).Value = item.Grade;
            ws1.Cell(i + 2, 4).Value = item.AssignedPromotor;
        }

        var ws2 = workbook.Worksheets.Add("Obłożenie Promotorów");
        ws2.Cell(1, 1).Value = "Promotor";
        ws2.Cell(1, 2).Value = "Limit";
        ws2.Cell(1, 3).Value = "Zajęte";
        ws2.Cell(1, 4).Value = "Wolne";
        ws2.Range("A1:D1").Style.Font.Bold = true;

        for (int i = 0; i < data.PromotorSummaries.Count; i++)
        {
            var p = data.PromotorSummaries[i];
            ws2.Cell(i + 2, 1).Value = p.Name;
            ws2.Cell(i + 2, 2).Value = p.Limit;
            ws2.Cell(i + 2, 3).Value = p.AssignedCount;
            ws2.Cell(i + 2, 4).Value = p.RemainingSlots;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] GeneratePdfReport(AdminReportDto data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(1, Unit.Centimetre);
                page.Header().Text("Raport Końcowy Przydziału Promotorów").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                page.Content().Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text("Podsumowanie Promotorów").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => {
                            c.RelativeColumn(3); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1);
                        });
                        table.Header(h => {
                            h.Cell().Text("Promotor"); h.Cell().Text("Limit"); h.Cell().Text("Zajęte"); h.Cell().Text("Wolne");
                        });
                        foreach (var p in data.PromotorSummaries)
                        {
                            table.Cell().Text(p.Name); table.Cell().Text(p.Limit.ToString());
                            table.Cell().Text(p.AssignedCount.ToString()); table.Cell().Text(p.RemainingSlots.ToString());
                        }
                    });

                    col.Item().PageBreak(); 

                    col.Item().Text("Pełna Lista Studentów").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => {
                            c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(2);
                        });
                        table.Header(h => {
                            h.Cell().Text("Student"); h.Cell().Text("Album"); h.Cell().Text("Promotor");
                        });
                        foreach (var s in data.AllAssignments)
                        {
                            table.Cell().Text(s.StudentName); table.Cell().Text(s.AlbumNumber); table.Cell().Text(s.AssignedPromotor);
                        }
                    });
                });
            });
        }).GeneratePdf();
    }
}