using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
                ErrorMessage = "Modyfikacja zespo³u jest mo¿liwa tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // backend clampuje 2–6, ale trzymamy spójnie w UI
                desiredSize = Math.Clamp(desiredSize, 2, 6);

                var resp = await client.PostAsJsonAsync("api/Teams/create", new { desiredSize });

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Utworzono zespó³.";
                    return RedirectToPage();
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    ErrorMessage = await resp.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(ErrorMessage))
                        ErrorMessage = "Nie mo¿na utworzyæ zespo³u (BadRequest).";
                    return RedirectToPage();
                }

                ErrorMessage = $"Nie uda³o siê utworzyæ zespo³u (HTTP {(int)resp.StatusCode}).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas tworzenia zespo³u.");
                ErrorMessage = "Wyst¹pi³ b³¹d podczas tworzenia zespo³u.";
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
                ErrorMessage = "Do³¹czanie do zespo³u jest mo¿liwe tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            if (teamId <= 0)
            {
                ErrorMessage = "Podaj poprawne ID zespo³u.";
                return RedirectToPage();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var resp = await client.PostAsync($"api/Teams/join/{teamId}", content: null);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = $"Do³¹czono do zespo³u {teamId}.";
                    return RedirectToPage();
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest ||
                    resp.StatusCode == HttpStatusCode.NotFound)
                {
                    var text = await resp.Content.ReadAsStringAsync();
                    ErrorMessage = string.IsNullOrWhiteSpace(text)
                        ? "Nie uda³o siê do³¹czyæ do zespo³u."
                        : text;
                    return RedirectToPage();
                }

                ErrorMessage = $"Nie uda³o siê do³¹czyæ do zespo³u (HTTP {(int)resp.StatusCode}).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas do³¹czania do zespo³u (TeamId={TeamId})", teamId);
                ErrorMessage = "Wyst¹pi³ b³¹d podczas do³¹czania do zespo³u.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostLeaveTeamAsync()
        {
            await LoadScheduleAsync();
            if (!CanEdit)
            {
                ErrorMessage = "Opuszczanie zespo³u jest mo¿liwe tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var resp = await client.PostAsync("api/Teams/leave", content: null);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Opuszczono zespó³.";
                    return RedirectToPage();
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest ||
                    resp.StatusCode == HttpStatusCode.NotFound)
                {
                    var text = await resp.Content.ReadAsStringAsync();
                    ErrorMessage = string.IsNullOrWhiteSpace(text)
                        ? "Nie uda³o siê opuœciæ zespo³u."
                        : text;
                    return RedirectToPage();
                }

                ErrorMessage = $"Nie uda³o siê opuœciæ zespo³u (HTTP {(int)resp.StatusCode}).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas opuszczania zespo³u.");
                ErrorMessage = "Wyst¹pi³ b³¹d podczas opuszczania zespo³u.";
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
                _logger.LogError(ex, "B³¹d podczas pobierania zespo³ów (api/Teams).");
                ErrorMessage ??= "Nie uda³o siê pobraæ listy zespo³ów.";
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
                _logger.LogError(ex, "B³¹d podczas pobierania statusu tury (api/Schedules).");
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