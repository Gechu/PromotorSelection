using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationDbContext _context;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public int? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public async Task<UserDto?> GetCurrentUserProfileAsync(CancellationToken ct = default)
    {
        var id = UserId;
        if (id == null) return null;

        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Promotor)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null) return null;

        return new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Student?.AlbumNumber,
            user.Student?.GradeAverage,
            user.Promotor?.StudentLimit,
            user.Student?.TeamId
        );
    }
}