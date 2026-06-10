using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromotorSelection.Services;

namespace PromotorSelection.Pages.Student
{
    [Authorize(Roles = "1")]
    public class TeamModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TeamModel> _logger;

        public TeamModel(IHttpClientFactory httpClientFactory, ILogger<TeamModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ScheduleStatusDto? ScheduleStatus { get; private set; }
        public bool CanEdit => ScheduleStatus?.IsActive == true;

        public List<TeamDto> Teams { get; private set; } = new();
        public TeamDto? MyTeam { get; private set; }
        public bool IsLeader => MyTeam is not null && MyTeam.LeaderId == CurrentUserId;

        private int? CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idStr, out var id)) return id;
                return null;
            }
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadScheduleAsync();
            await LoadTeamsAsync();
            ComputeMyTeam();
        }

        public async Task<IActionResult> OnPostCreateTeamAsync([FromForm] int desiredSize)
        {
            await LoadScheduleAsync();
            if (!CanEdit)
            {
                ErrorMessage = "Modyfikacja zespołu jest możliwa tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // backend clampuje 2-6, ale trzymamy spójnie w UI
                desiredSize = Math.Clamp(desiredSize, 2, 6);

                var resp = await client.PostAsJsonAsync("api/Teams/create", new { desiredSize });

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Utworzono zespół.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia zespołu.");
                ErrorMessage = "Wystąpił błąd podczas tworzenia zespołu.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostJoinTeamAsync([FromForm] int teamId)
        {
            return await JoinInternalAsync(teamId);
        }

        public async Task<IActionResult> OnPostJoinByIdAsync([FromForm] int teamId)
        {
            return await JoinInternalAsync(teamId);
        }

        private async Task<IActionResult> JoinInternalAsync(int teamId)
        {
            await LoadScheduleAsync();
            if (!CanEdit)
            {
                ErrorMessage = "Dołączanie do zespołu jest możliwe tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            if (teamId <= 0)
            {
                ErrorMessage = "Podaj poprawne ID zespołu.";
                return RedirectToPage();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var resp = await client.PostAsync($"api/Teams/join/{teamId}", content: null);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = $"Dołączono do zespołu {teamId}.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dołączania do zespołu (TeamId={TeamId})", teamId);
                ErrorMessage = "Wystąpiłą błąd podczas dołączania do zespołu.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostLeaveTeamAsync()
        {
            await LoadScheduleAsync();
            if (!CanEdit)
            {
                ErrorMessage = "Opuszczanie zespołu jest możliwe tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var resp = await client.PostAsync("api/Teams/leave", content: null);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Opuszczono zespół.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas opuszczania zespołu.");
                ErrorMessage = "Wystąpił błąd podczas opuszczania zespołu.";
                return RedirectToPage();
            }
        }

        private async Task LoadTeamsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                Teams = await client.GetFromJsonAsync<List<TeamDto>>("api/Teams") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zespołów (api/Teams).");
                ErrorMessage ??= "Nie udało się pobrać listy zespołów.";
            }
        }

        private async Task LoadScheduleAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                ScheduleStatus = await client.GetFromJsonAsync<ScheduleStatusDto>("api/Schedules");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania statusu tury (api/Schedules).");
            }
        }

        private void ComputeMyTeam()
        {
            var userId = CurrentUserId;
            if (userId is null)
            {
                return;
            }

            MyTeam = Teams.FirstOrDefault(t => t.Members.Any(m => m.UserId == userId.Value));
        }

        // ===== DTOs =====
        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public class TeamMemberDto
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
        }

        public class TeamDto
        {
            public int Id { get; set; }
            public int TeamSize { get; set; } // -1 => closed
            public int LeaderId { get; set; }
            public int CurrentMembersCount { get; set; }
            public bool IsClosed => TeamSize == -1;
            public List<TeamMemberDto> Members { get; set; } = new();
        }
    }
}