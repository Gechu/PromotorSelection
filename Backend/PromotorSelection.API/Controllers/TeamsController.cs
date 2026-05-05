using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using PromotorSelection.Application.Teams;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.API.Controllers;

[Authorize(Roles = "1,3")]
[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TeamsController(IMediator mediator) => _mediator = mediator;
    
    
    [HttpGet]
    public async Task<ActionResult<List<TeamDto>>> GetAllTeams()
    {
        return Ok(await _mediator.Send(new GetTeamsQuery()));
    }

    [HttpPost("create")]
    public async Task<ActionResult<int>> Create([FromBody] CreateTeamCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("join/{teamId}")]
    public async Task<ActionResult<bool>> Join(int teamId)
    {
        return Ok(await _mediator.Send(new JoinTeamCommand(teamId)));
    }

    [HttpPost("leave")]
    public async Task<ActionResult<bool>> Leave()
    {
        return Ok(await _mediator.Send(new LeaveTeamCommand()));
    }
}