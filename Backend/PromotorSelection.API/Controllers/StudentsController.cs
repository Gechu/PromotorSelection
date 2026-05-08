using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Students;
using PromotorSelection.Application.Users;

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

    [Authorize(Roles = "3")]
    [HttpPost]
    public async Task<ActionResult<int>> ImportStudents(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nie przesłano pliku.");
        using var stream = file.OpenReadStream();

        var result = await _mediator.Send(new ImportStudentsCommand(stream));

        return Ok(result);
    }

}