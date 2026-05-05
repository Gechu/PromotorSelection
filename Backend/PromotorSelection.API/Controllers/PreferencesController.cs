using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using PromotorSelection.Application.Preferences;

namespace PromotorSelection.API.Controllers;

[Authorize(Roles = "1,3")]
[ApiController]
[Route("api/[controller]")]
public class PreferencesController : ControllerBase
{
    private readonly IMediator _mediator;
    public PreferencesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<bool>> SetPreferences([FromBody] SetPreferencesCommand command)
    {
        return Ok(await _mediator.Send(command));
    }
}