using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using PromotorSelection.Application.Schedules;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISystemStatusService _statusService;

    public SchedulesController(IMediator mediator, ISystemStatusService statusService)
    {
        _mediator = mediator;
        _statusService = statusService;
    }

    [Authorize(Roles = "3")]
    [HttpPost]
    public async Task<IActionResult> UpdateSchedule([FromBody] UpdateScheduleCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        return Ok(new
        {
            IsActive = await _statusService.IsSystemActiveAsync(),
            Message = await _statusService.GetCurrentStatusMessageAsync()
        });
    }
}