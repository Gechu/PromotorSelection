using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Students;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetStudentsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [Authorize(Roles = "1,3")]
    [HttpPut]
    public async Task<ActionResult<bool>> UpdateGrade([FromBody] UpdateGradeCommand command)
    {
        return Ok(await _mediator.Send(command));
    }


}