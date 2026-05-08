using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using PromotorSelection.Application.Statistics;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "3")] 
public class ReportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReportService _reportService;

    public ReportController(IMediator mediator, IReportService reportService)
    {
        _mediator = mediator;
        _reportService = reportService;
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportAllToExcel()
    {
        var data = await _mediator.Send(new GetAdminReportQuery());

        var fileContents = _reportService.GenerateExcelReport(data);

        return File(
            fileContents,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Raport_Przydzialow_{DateTime.Now:yyyyMMdd}.xlsx"
        );
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportAllToPdf()
    {
        var data = await _mediator.Send(new GetAdminReportQuery());

        var fileContents = _reportService.GeneratePdfReport(data);

        return File(
            fileContents,
            "application/pdf",
            $"Raport_Przydzialow_{DateTime.Now:yyyyMMdd}.pdf"
        );
    }
}