using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Student
{
    [Authorize(Roles = "1")]
    public class GradesModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GradesModel> _logger;

        public GradesModel(IHttpClientFactory httpClientFactory, ILogger<GradesModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ScheduleStatusDto? ScheduleStatus { get; private set; }

        public double? CurrentGrade { get; private set; }

        public string? CurrentGradeDisplay
            => CurrentGrade.HasValue ? CurrentGrade.Value.ToString("0.00", CultureInfo.InvariantCulture) : null;

        public bool CanEdit
            => ScheduleStatus?.IsActive == true;

        public string? EditBlockReason
        {
            get
            {
                if (ScheduleStatus is null) return "Nie uda³o siê pobraæ statusu tury — spróbuj ponownie póŸniej.";
                if (!ScheduleStatus.IsActive) return "Edycja jest dostêpna tylko w trakcie aktywnej tury wyborów.";
                return null;
            }
        }

        [BindProperty] public GradeForm Form { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadScheduleAsync();
            await LoadCurrentGradeAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadScheduleAsync();
            await LoadCurrentGradeAsync();

            if (!CanEdit)
            {
                ErrorMessage = "Nie mo¿na zmieniæ œredniej: tura wyborów jest nieaktywna.";
                return Page();
            }

            // Parsowanie: 4,56 i 4.56
            var raw = (Form.NewGrade ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                ErrorMessage = "Podaj wartoœæ œredniej.";
                return Page();
            }

            raw = raw.Replace(',', '.');

            if (!double.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var newGrade))
            {
                ErrorMessage = "Nie uda³o siê odczytaæ œredniej. Wpisz np. 4.56 lub 4,56.";
                return Page();
            }

            if (newGrade is < 2.0 or > 5.5)
            {
                ErrorMessage = "Œrednia musi byæ w zakresie 2.0 – 5.5.";
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Backend: PUT /api/Students (UpdateGradeCommand { newGrade })
                var resp = await client.PutAsJsonAsync("api/Students", new { newGrade });

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Zapisano œredni¹.";
                    return RedirectToPage();
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Backend czêsto zwraca czytelny komunikat
                    var text = await resp.Content.ReadAsStringAsync();
                    ErrorMessage = string.IsNullOrWhiteSpace(text)
                        ? "Nie mo¿na zapisaæ œredniej (BadRequest)."
                        : text;
                    return Page();
                }

                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Nie znaleziono profilu studenta.";
                    return Page();
                }

                ErrorMessage = $"Nie uda³o siê zapisaæ œredniej (HTTP {(int)resp.StatusCode}).";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisu œredniej studenta.");
                ErrorMessage = "Wyst¹pi³ b³¹d podczas zapisu œredniej.";
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
                _logger.LogError(ex, "B³¹d podczas pobierania statusu tury (api/Schedules).");
            }
        }

        private async Task LoadCurrentGradeAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Najproœciej: bierzemy bie¿¹cego studenta z listy studentów po emailu.
                // Jeœli masz endpoint "me", to warto go u¿yæ zamiast tego.
                var meEmail = User?.Identity?.Name;

                var all = await client.GetFromJsonAsync<List<StudentDto>>("api/Students") ?? new();

                var me = !string.IsNullOrWhiteSpace(meEmail)
                    ? all.FirstOrDefault(s => string.Equals(s.Email, meEmail, StringComparison.OrdinalIgnoreCase))
                    : null;

                CurrentGrade = me?.GradeAverage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania aktualnej œredniej (api/Students).");
            }
        }

        public class GradeForm
        {
            public string? NewGrade { get; set; }
        }

        // z /api/Schedules
        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        // minimalny DTO zgodny z tym, co zwraca api/Students (GetStudentsQuery mapuje User + Student)
        public class StudentDto
        {
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public double? GradeAverage { get; set; }
        }
    }
}