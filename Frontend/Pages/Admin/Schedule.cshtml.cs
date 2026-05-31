using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromotorSelection.Services;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "3")]
    public class ScheduleModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ScheduleModel> _logger;

        public ScheduleModel(IHttpClientFactory httpClientFactory, ILogger<ScheduleModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ScheduleStatusDto? Status { get; private set; }

        [BindProperty]
        public ScheduleForm Form { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStatusAndPrefillAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadStatusOnlyAsync();
                return Page();
            }

            // UI-walidacja
            if (Form.StartDate >= Form.EndDate)
            {
                ModelState.AddModelError(string.Empty, "Data rozpocz�cia musi by� wcze�niejsza ni� data zako�czenia.");
                await LoadStatusOnlyAsync();
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                var resp = await client.PostAsJsonAsync("api/Schedules/change-schedule", new
                {
                    startDate = Form.StartDate,
                    endDate = Form.EndDate
                });

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Harmonogram zosta� zapisany.";
                    return RedirectToPage();
                }

                ErrorMessage = ErrorTranslator.Translate(resp);

                await LoadStatusOnlyAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas zapisu harmonogramu.");
                ErrorMessage = "Wyst�pi� b��d podczas zapisu harmonogramu.";
                await LoadStatusOnlyAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRunAllocationAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                var resp = await client.PostAsync("api/Schedules/run-allocation", content: null);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Uruchomiono przydzia�.";
                    return RedirectToPage();
                }

                ErrorMessage = ErrorTranslator.Translate(resp);

                await LoadStatusOnlyAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas uruchamiania przydzia�u.");
                ErrorMessage = "Wyst�pi� b��d podczas uruchamiania przydzia�u.";
                await LoadStatusOnlyAsync();
                return Page();
            }
        }

        private async Task LoadStatusOnlyAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                Status = await client.GetFromJsonAsync<ScheduleStatusDto>("api/Schedules");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas pobierania statusu harmonogramu.");
            }
        }

        private async Task LoadStatusAndPrefillAsync()
        {
            await LoadStatusOnlyAsync();

            if (Status?.StartDate is not null)
                Form.StartDate = Status.StartDate.Value;

            if (Status?.EndDate is not null)
                Form.EndDate = Status.EndDate.Value;
        }

        public class ScheduleForm
        {
            [Required]
            public DateTime StartDate { get; set; }

            [Required]
            public DateTime EndDate { get; set; }
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