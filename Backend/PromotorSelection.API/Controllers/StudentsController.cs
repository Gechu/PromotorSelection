using MediatR;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Students;

namespace PromotorSelection.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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


    }
}