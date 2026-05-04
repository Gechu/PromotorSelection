using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Users;
using PromotorSelection.Application.Dto;
using Microsoft.AspNetCore.Authorization;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "3")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers() =>
        Ok(await _mediator.Send(new GetUsersQuery()));

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUsers), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.UserId) return BadRequest();

        var result = await _mediator.Send(command);

        if (!result) return NotFound();
        return NoContent();
    }

   
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _mediator.Send(new DeleteUserCommand(id));
        return NoContent();
    }
}