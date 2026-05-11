using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
                ModelState.AddModelError(string.Empty, "Data rozpoczêcia musi byæ wczeœniejsza ni¿ data zakoñczenia.");
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
                    SuccessMessage = "Harmonogram zosta³ zapisany.";
                    return RedirectToPage();
                }

                ErrorMessage = await BuildNiceErrorMessageAsync(
                    resp,
                    fallback: "Nie uda³o siê zapisaæ harmonogramu. SprawdŸ poprawnoœæ dat i spróbuj ponownie."
                );

                await LoadStatusOnlyAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisu harmonogramu.");
                ErrorMessage = "Wyst¹pi³ b³¹d podczas zapisu harmonogramu.";
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
                    SuccessMessage = "Uruchomiono przydzia³.";
                    return RedirectToPage();
                }

                // Specjalny, zrozumia³y komunikat dla typowego przypadku:
                // uruchomienie w trakcie tury
                if (resp.StatusCode == HttpStatusCode.BadRequest || resp.StatusCode == HttpStatusCode.Conflict)
                {
                    ErrorMessage = "Przydzia³ mo¿na uruchomiæ dopiero po zakoñczeniu terminu wyborów.";
                }
                else
                {
                    ErrorMessage = await BuildNiceErrorMessageAsync(
                        resp,
                        fallback: "Nie uda³o siê uruchomiæ przydzia³u. Spróbuj ponownie póŸniej."
                    );
                }

                await LoadStatusOnlyAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas uruchamiania przydzia³u.");
                ErrorMessage = "Wyst¹pi³ b³¹d podczas uruchamiania przydzia³u.";
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
                _logger.LogError(ex, "B³¹d podczas pobierania statusu harmonogramu.");
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

        private static async Task<string> BuildNiceErrorMessageAsync(HttpResponseMessage resp, string fallback)
        {
            string body = string.Empty;
            try
            {
                body = await resp.Content.ReadAsStringAsync();
            }
            catch
            {
                // ignore
            }

            // 1) Spróbujmy JSON
            var msgFromJson = TryExtractErrorFromJson(body);
            if (!string.IsNullOrWhiteSpace(msgFromJson))
                return msgFromJson;

            // 2) Jeœli backend zwróci³ plain-text
            if (!string.IsNullOrWhiteSpace(body) && body.Length < 300 && !body.TrimStart().StartsWith("{"))
                return body.Trim();

            // 3) Fallback zale¿ny od statusu
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return "Brak autoryzacji. Zaloguj siê ponownie.";

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                return "Brak uprawnieñ do wykonania tej operacji.";

            if ((int)resp.StatusCode >= 500)
                return "Wyst¹pi³ b³¹d serwera. Spróbuj ponownie póŸniej.";

            return fallback;
        }

        private static string? TryExtractErrorFromJson(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            body = body.Trim();
            if (!body.StartsWith("{") && !body.StartsWith("["))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // format: { "error": "..." }
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("error", out var errProp) &&
                    errProp.ValueKind == JsonValueKind.String)
                {
                    return errProp.GetString();
                }

                // format: { "errors": [ { "PropertyName": "...", "ErrorMessage": "..." }, ... ] }
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("errors", out var errorsProp) &&
                    errorsProp.ValueKind == JsonValueKind.Array)
                {
                    var msgs = new List<string>();
                    foreach (var item in errorsProp.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;

                        string? message = null;

                        if (item.TryGetProperty("ErrorMessage", out var em) && em.ValueKind == JsonValueKind.String)
                            message = em.GetString();

                        // czasem pola mog¹ mieæ inne nazwy
                        if (string.IsNullOrWhiteSpace(message) &&
                            item.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                            message = m.GetString();

                        if (!string.IsNullOrWhiteSpace(message))
                            msgs.Add(message!);
                    }

                    if (msgs.Count > 0)
                        return string.Join(" ", msgs.Distinct());
                }
            }
            catch
            {
                // nie parsowalne / inny format
            }

            return null;
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