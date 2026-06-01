using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromotorSelection.Services;

namespace PromotorSelection.Pages.Student
{
    [Authorize(Roles = "1")]
    public class PreferencesModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PreferencesModel> _logger;

        public PreferencesModel(IHttpClientFactory httpClientFactory, ILogger<PreferencesModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ScheduleStatusDto? ScheduleStatus { get; private set; }

        public List<PromotorSelectItem> PromotorsForSelect { get; private set; } = new();

        public List<TeamDto> Teams { get; private set; } = new();
        public TeamDto? MyTeam { get; private set; }

        public bool IsInTeam => MyTeam is not null;
        public bool IsLeader => MyTeam is not null && MyTeam.LeaderId == CurrentUserId;

        public bool CanEdit => ScheduleStatus?.IsActive == true && (!IsInTeam || IsLeader);

        // NOWE: preferencje z backendu (GET)
        public List<SelectedPreferenceItem>? SelectedPreferences { get; private set; } = null;

        [BindProperty] public PreferenceForm Form { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        private int? CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idStr, out var id) ? id : null;
            }
        }

        public async Task OnGetAsync()
        {
            await LoadScheduleAsync();
            await LoadTeamsAsync();
            ComputeMyTeam();

            await LoadPromotorsForSelectAsync();
            await LoadSelectedPreferencesAsync();

            // Opcjonalnie: jeśli mamy zapisane preferencje, można prefillować selecty
            // (tylko jako UX, nie jest wymagane do samego "podglądu")
            PrefillFormFromSelectedPreferences();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadScheduleAsync();
            await LoadTeamsAsync();
            ComputeMyTeam();
            await LoadPromotorsForSelectAsync();
            await LoadSelectedPreferencesAsync(); // żeby nadal pokazywać podgląd po błędach

            if (!CanEdit)
            {
                ErrorMessage = IsInTeam && !IsLeader
                    ? "Tylko lider zespołu może zapisać preferencje."
                    : "Modyfikacja preferencji jest możliwa tylko w trakcie aktywnej tury.";
                return Page();
            }

            if (Form.P1 <= 0 || Form.P2 <= 0 || Form.P3 <= 0)
            {
                ErrorMessage = "Wybierz wszystkie 3 preferencje.";
                return Page();
            }

            if (Form.P1 == Form.P2 || Form.P1 == Form.P3 || Form.P2 == Form.P3)
            {
                ErrorMessage = "Preferencje nie mogą się powtarzać.";
                return Page();
            }

            var promotorIds = new List<int> { Form.P1, Form.P2, Form.P3 };

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Backend: POST /api/Preferences { promotorIds: [..3..] }
                var resp = await client.PostAsJsonAsync("api/Preferences", new { promotorIds });

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = IsInTeam ? "Zapisano preferencje dla zespołu." : "Zapisano preferencje.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas zapisu preferencji.");
                ErrorMessage = "Wystąpił błąd podczas zapisu preferencji.";
                return Page();
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
            }
        }

        private void ComputeMyTeam()
        {
            var uid = CurrentUserId;
            if (uid is null) return;

            MyTeam = Teams.FirstOrDefault(t => t.Members.Any(m => m.UserId == uid.Value));
        }

        private async Task LoadPromotorsForSelectAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var promotors = await client.GetFromJsonAsync<List<PromotorDto>>("api/Promotors") ?? new();

                PromotorsForSelect = promotors
                    .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                    .Select(p => new PromotorSelectItem
                    {
                        UserId = p.UserId,
                        Label = $"{p.LastName} {p.FirstName} ({p.Email})"
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania promotorów do selectów (api/Promotors).");
                ErrorMessage ??= "Nie udało się pobrać listy promotorów.";
            }
        }

        /// <summary>
        /// NOWE: pobranie aktualnie zapisanych preferencji z backendu.
        /// Zakładamy endpoint: GET api/Preferences (dla zalogowanego studenta / teamu).
        /// </summary>
        private async Task LoadSelectedPreferencesAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Jeśli u Ciebie endpoint ma inną ścieżkę, zmień ją tutaj:
                // np. "api/Preferences/me"
                var resp = await client.GetAsync("api/Preferences");

                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    // Jeśli backend zwraca 404 gdy brak zapisanych preferencji:
                    SelectedPreferences = new List<SelectedPreferenceItem>();
                    return;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    // Nie blokuj strony — pokazujemy formularz, a podgląd może być pusty.
                    ErrorMessage ??= await ErrorTranslator.TranslateAsync(resp);
                    SelectedPreferences = null;
                    return;
                }

                var dto = await resp.Content.ReadFromJsonAsync<List<PreferenceGetDto>>();
                if (dto is null)
                {
                    SelectedPreferences = new List<SelectedPreferenceItem>();
                    return;
                }

                SelectedPreferences = dto
                    .Select(x => new SelectedPreferenceItem
                    {
                        Priority = x.Priority,
                        PromotorId = x.PromotorId,
                        PromotorName = BuildPromotorName(x)
                    })
                    .OrderBy(x => x.Priority)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania aktualnych preferencji (GET api/Preferences).");
                // nie blokujemy, tylko nie pokazujemy listy
                SelectedPreferences = null;
            }
        }

        private static string BuildPromotorName(PreferenceGetDto x)
        {
            // Najczęściej backend oddaje imię/nazwisko promotora.
            // Jeśli u Ciebie jest inaczej, podmień to mapowanie.
            var name = $"{x.PromotorLastName} {x.PromotorFirstName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? $"PromotorId={x.PromotorId}" : name;
        }

        private void PrefillFormFromSelectedPreferences()
        {
            if (SelectedPreferences is null || SelectedPreferences.Count == 0)
                return;

            // Uwaga: jeśli zapisane są inne priorytety niż 1..3, to i tak defensywnie mapujemy.
            Form.P1 = SelectedPreferences.FirstOrDefault(p => p.Priority == 1)?.PromotorId ?? Form.P1;
            Form.P2 = SelectedPreferences.FirstOrDefault(p => p.Priority == 2)?.PromotorId ?? Form.P2;
            Form.P3 = SelectedPreferences.FirstOrDefault(p => p.Priority == 3)?.PromotorId ?? Form.P3;
        }

        // ===== Forms / DTOs =====
        public class PreferenceForm
        {
            public int P1 { get; set; }
            public int P2 { get; set; }
            public int P3 { get; set; }
        }

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
            public int TeamSize { get; set; }
            public int LeaderId { get; set; }
            public int CurrentMembersCount { get; set; }
            public bool IsClosed => TeamSize == -1;
            public List<TeamMemberDto> Members { get; set; } = new();
        }

        public class PromotorDto
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class PromotorSelectItem
        {
            public int UserId { get; set; }
            public string Label { get; set; } = string.Empty;
        }

        // DTO zgodne z GET /api/Preferences (dostosuj nazwy pól jeśli inne)
        public class PreferenceGetDto
        {
            public int Priority { get; set; }
            public int PromotorId { get; set; }

            // jeśli backend zwraca name — super:
            public string PromotorFirstName { get; set; } = string.Empty;
            public string PromotorLastName { get; set; } = string.Empty;
        }

        public class SelectedPreferenceItem
        {
            public int Priority { get; set; }
            public int PromotorId { get; set; }
            public string PromotorName { get; set; } = string.Empty;
        }
    }
}