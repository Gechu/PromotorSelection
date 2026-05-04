using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.API.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public AccountController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUserInfo(CancellationToken ct)
    {
        var userProfile = await _currentUserService.GetCurrentUserProfileAsync(ct);

        return Ok(userProfile);
    }
}