using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromotorSelection.Services;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "3")]
    public class ReportsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReportsModel> _logger;

        public ReportsModel(IHttpClientFactory httpClientFactory, ILogger<ReportsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ScheduleStatusDto? Status { get; private set; }

        // NOWE: statystyki systemowe
        public StatisticsDto? Stats { get; private set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public bool HasSchedule
            => Status?.StartDate is not null && Status?.EndDate is not null;

        public bool CanRunAllocation
            => Status?.EndDate is not null && DateTime.Now > Status.EndDate.Value;

        public string? TimeToEndDisplay
        {
            get
            {
                if (Status?.EndDate is null) return null;

                var now = DateTime.Now;
                var end = Status.EndDate.Value;

                if (now >= end) return "0m";

                var diff = end - now;

                if (diff.TotalDays >= 1)
                    return $"{(int)diff.TotalDays}d {diff.Hours}h";

                if (diff.TotalHours >= 1)
                    return $"{(int)diff.TotalHours}h {diff.Minutes}m";

                return $"{diff.Minutes}m";
            }
        }

        public async Task OnGetAsync()
        {
            await LoadStatusAsync();
            await LoadStatsAsync();
        }

        // ===== Przydzia� =====
        public async Task<IActionResult> OnPostRunAllocationAsync()
        {
            await LoadStatusAsync();

            if (!HasSchedule)
            {
                ErrorMessage = "Nie mo�na uruchomi� przydzia�u: harmonogram nie jest ustawiony.";
                return Page();
            }

            if (!CanRunAllocation)
            {
                ErrorMessage = "Przydzia� mo�na uruchomi� dopiero po zako�czeniu terminu wybor�w.";
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                var resp = await client.PostAsync("api/Schedules/run-allocation", content: null);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Uruchomiono przydzia�. Poni�ej statystyki oraz eksport PDF/XLSX.";

                    // NOWE: po przydziale od razu pobierz statystyki
                    await LoadStatsAsync();

                    // Zwracamy Page(), �eby od razu pokaza� sekcj� statystyk.
                    return Page();
                }

                ErrorMessage = ErrorTranslator.Translate(resp);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas uruchamiania przydzia�u.");
                ErrorMessage = "Wyst�pi� b��d podczas uruchamiania przydzia�u.";
                return Page();
            }
        }

        // ===== Eksporty (proxy przez frontend) =====
        public async Task<IActionResult> OnGetPdfAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var bytes = await client.GetByteArrayAsync("api/Report/pdf");

                var fileName = $"Raport_Przydzialow_{DateTime.Now:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas pobierania raportu PDF.");
                ErrorMessage = "Nie uda�o si� wygenerowa�/pobra� raportu PDF.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetExcelAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var bytes = await client.GetByteArrayAsync("api/Report/excel");

                var fileName = $"Raport_Przydzialow_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas pobierania raportu Excel.");
                ErrorMessage = "Nie uda�o si� wygenerowa�/pobra� raportu Excel.";
                return RedirectToPage();
            }
        }

        // ===== Helpers =====
        private async Task LoadStatusAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                Status = await client.GetFromJsonAsync<ScheduleStatusDto>("api/Schedules");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas pobierania statusu harmonogramu (api/Schedules).");
            }
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                Stats = await client.GetFromJsonAsync<StatisticsDto>("api/Statistics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas pobierania statystyk (api/Statistics).");
                // nie blokujemy strony, ale poka�emy alert
                ErrorMessage ??= "Nie uda�o si� pobra� statystyk po przydziale.";
            }
        }

        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        // DTO pod /api/Statistics
        public class StatisticsDto
        {
            public int TotalTeams { get; set; }
            public int FreelancersCount { get; set; }
            public int IdleStudentsCount { get; set; }
            public List<PromotorOccupancyDto> PromotorOccupancy { get; set; } = new();
        }

        public class PromotorOccupancyDto
        {
            public int PromotorId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public int StudentLimit { get; set; }
            public int InterestedStudentsCount { get; set; }
        }
    }
}