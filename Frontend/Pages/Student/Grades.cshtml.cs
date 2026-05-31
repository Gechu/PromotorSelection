using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromotorSelection.Services;

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
                if (ScheduleStatus is null) return "Nie uda�o si� pobra� statusu tury � spr�buj ponownie p�niej.";
                if (!ScheduleStatus.IsActive) return "Edycja jest dost�pna tylko w trakcie aktywnej tury wybor�w.";
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
                ErrorMessage = "Nie mo�na zmieni� �redniej: tura wybor�w jest nieaktywna.";
                return Page();
            }

            // Parsowanie: 4,56 i 4.56
            var raw = (Form.NewGrade ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                ErrorMessage = "Podaj warto�� �redniej.";
                return Page();
            }

            raw = raw.Replace(',', '.');

            if (!double.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var newGrade))
            {
                ErrorMessage = "Nie uda�o si� odczyta� �redniej. Wpisz np. 4.56 lub 4,56.";
                return Page();
            }

            if (newGrade is < 2.0 or > 5.5)
            {
                ErrorMessage = "�rednia musi by� w zakresie 2.0 � 5.5.";
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Backend: PUT /api/Students (UpdateGradeCommand { newGrade })
                var resp = await client.PutAsJsonAsync("api/Students", new { newGrade });

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Zapisano �redni�.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas zapisu �redniej studenta.");
                ErrorMessage = "Wyst�pi� b��d podczas zapisu �redniej.";
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
                _logger.LogError(ex, "B��d podczas pobierania statusu tury (api/Schedules).");
            }
        }

        private async Task LoadCurrentGradeAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Bezpieczny endpoint � pobieramy profil zalogowanego u�ytkownika
                var userProfile = await client.GetFromJsonAsync<UserProfileDto>("api/Account");

                CurrentGrade = userProfile?.GradeAverage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas pobierania aktualnej �redniej (api/Account).");
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

        // DTO zgodny z /api/Account
        public class UserProfileDto
        {
            public int Id { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? AlbumNumber { get; set; }
            public double? GradeAverage { get; set; }
            public int? TeamId { get; set; }
            public int? StudentLimit { get; set; }
        }
    }
}