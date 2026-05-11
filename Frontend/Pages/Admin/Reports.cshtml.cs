using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

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
        }

        // ===== Przydzia³ =====

        public async Task<IActionResult> OnPostRunAllocationAsync()
        {
            await LoadStatusAsync();

            if (!HasSchedule)
            {
                ErrorMessage = "Nie mo¿na uruchomiæ przydzia³u: harmonogram nie jest ustawiony.";
                return Page();
            }

            if (!CanRunAllocation)
            {
                ErrorMessage = "Przydzia³ mo¿na uruchomiæ dopiero po zakoñczeniu terminu wyborów.";
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                var resp = await client.PostAsync("api/Schedules/run-allocation", content: null);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Uruchomiono przydzia³. Mo¿esz teraz wygenerowaæ raport PDF/XLSX.";
                    return RedirectToPage();
                }

                // Nie pokazujemy surowego JSON z backendu
                ErrorMessage = "Nie uda³o siê uruchomiæ przydzia³u. Spróbuj ponownie póŸniej (lub sprawdŸ logi backendu).";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas uruchamiania przydzia³u.");
                ErrorMessage = "Wyst¹pi³ b³¹d podczas uruchamiania przydzia³u.";
                return Page();
            }
        }

        // ===== Eksporty (proxy przez frontend) =====

        public async Task<IActionResult> OnGetPdfAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // pobieramy jako bytes i zwracamy z frontu jako plik
                var bytes = await client.GetByteArrayAsync("api/Report/pdf");

                var fileName = $"Raport_Przydzialow_{DateTime.Now:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania raportu PDF.");
                ErrorMessage = "Nie uda³o siê wygenerowaæ/pobraæ raportu PDF.";
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
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania raportu Excel.");
                ErrorMessage = "Nie uda³o siê wygenerowaæ/pobraæ raportu Excel.";
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
                _logger.LogError(ex, "B³¹d podczas pobierania statusu harmonogramu (api/Schedules).");
            }
        }

        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }
    }
}