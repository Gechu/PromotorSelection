using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Topics;
using PromotorSelection.Application.Dto;
using Microsoft.AspNetCore.Authorization;

namespace PromotorSelection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "2,3")]
public class TopicsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TopicsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TopicDto>>> GetAll()
        => Ok(await _mediator.Send(new GetTopicsQuery()));

    [HttpPost]
    public async Task<ActionResult<TopicDto>> Create([FromBody] CreateTopicCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTopicCommand command)
    {
        if (id != command.Id) return BadRequest();
        return await _mediator.Send(command) ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
        => await _mediator.Send(new DeleteTopicCommand(id)) ? NoContent() : NotFound();
}