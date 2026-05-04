using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }

        Task<UserDto?> GetCurrentUserProfileAsync(CancellationToken ct = default);
    }
}
