using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using PromotorSelection.Application.Statistics;
using PromotorSelection.Application.Dto;
using PromotorSelection.Application.Allocations;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatisticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<StatisticsDto>> GetSystemStatistics()
    {
        var stats = await _mediator.Send(new GetSystemStatisticsQuery());
        return Ok(stats);
    }

    [HttpGet("all-allocations")]
    [Authorize(Roles = "3")]
    public async Task<ActionResult<IEnumerable<AllocationResultDto>>> GetAllAllocations()
    {
        var results = await _mediator.Send(new GetAllAllocationsQuery());
        return Ok(results);
    }

    [HttpGet("promotor-allocations")]
    [Authorize(Roles = "2")]
    public async Task<ActionResult<IEnumerable<AllocationResultDto>>> GetMyAllocations()
    {
        var results = await _mediator.Send(new GetPromotorAllocationsQuery());
        return Ok(results);
    }
}