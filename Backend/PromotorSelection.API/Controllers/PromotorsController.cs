using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Promotors;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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