using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Common.Interfaces;

public interface IReportService
{
    byte[] GenerateExcelReport(AdminReportDto data);
    byte[] GeneratePdfReport(AdminReportDto data);
}