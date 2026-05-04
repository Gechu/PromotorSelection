using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Promotors;
using PromotorSelection.Application.Dto;
using Microsoft.AspNetCore.Authorization;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PromotorsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PromotorsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PromotorDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetPromotorsQuery()));
    }

}