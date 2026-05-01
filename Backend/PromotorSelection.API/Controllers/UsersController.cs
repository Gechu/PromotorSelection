using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Users;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _mediator.Send(new DeleteUserCommand(id));
        return NoContent();
    }
}