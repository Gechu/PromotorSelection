using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromotorSelection.Services;

namespace PromotorSelection.Pages.Promotor
{
    [Authorize(Roles = "2")]
    public class TopicsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TopicsModel> _logger;

        public TopicsModel(IHttpClientFactory httpClientFactory, ILogger<TopicsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ScheduleStatusDto? ScheduleStatus { get; private set; }
        public bool CanEdit => ScheduleStatus?.IsActive == true;

        public List<TopicDto> Topics { get; private set; } = new();

        public bool IsEditMode { get; private set; }

        [BindProperty] public TopicForm Form { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync([FromQuery] int? editId)
        {
            await LoadScheduleAsync();
            await LoadTopicsAsync();

            // Je�li editId jest podane, za�aduj ten temat do formularza
            if (editId.HasValue && editId.Value > 0)
            {
                var topic = Topics.FirstOrDefault(t => t.Id == editId.Value);
                if (topic is not null)
                {
                    IsEditMode = true;
                    Form.Id = topic.Id;
                    Form.Title = topic.Title;
                    Form.Description = topic.Description;
                }
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            await LoadScheduleAsync();
            if (!CanEdit)
            {
                ErrorMessage = "Dodawanie temat�w jest mo�liwe tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Form.Title))
            {
                ErrorMessage = "Tytu� tematu jest wymagany.";
                await LoadTopicsAsync();
                return Page();
            }

            if (Form.Title.Length > 50)
            {
                ErrorMessage = "Tytu� nie mo�e by� d�u�szy ni� 50 znak�w.";
                await LoadTopicsAsync();
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Form.Description) && Form.Description.Length > 200)
            {
                ErrorMessage = "Opis nie mo�e by� d�u�szy ni� 200 znak�w.";
                await LoadTopicsAsync();
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                var payload = new
                {
                    title = Form.Title.Trim(),
                    description = Form.Description?.Trim() ?? string.Empty
                };

                var resp = await client.PostAsJsonAsync("api/Topics", payload);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Dodano nowy temat.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                await LoadTopicsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas dodawania tematu.");
                ErrorMessage = "Wyst�pi� b��d podczas dodawania tematu.";
                await LoadTopicsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromForm] int topicId)
        {
            await LoadScheduleAsync();
            if (!CanEdit)
            {
                ErrorMessage = "Edycja temat�w jest mo�liwa tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Form.Title))
            {
                ErrorMessage = "Tytu� tematu jest wymagany.";
                await LoadTopicsAsync();
                Form.Id = topicId;
                IsEditMode = true;
                return Page();
            }

            if (Form.Title.Length > 50)
            {
                ErrorMessage = "Tytu� nie mo�e by� d�u�szy ni� 50 znak�w.";
                await LoadTopicsAsync();
                Form.Id = topicId;
                IsEditMode = true;
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Form.Description) && Form.Description.Length > 200)
            {
                ErrorMessage = "Opis nie mo�e by� d�u�szy ni� 200 znak�w.";
                await LoadTopicsAsync();
                Form.Id = topicId;
                IsEditMode = true;
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                var payload = new
                {
                    id = topicId,
                    title = Form.Title.Trim(),
                    description = Form.Description?.Trim() ?? string.Empty
                };

                var resp = await client.PutAsJsonAsync($"api/Topics/{topicId}", payload);

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Temat zaktualizowany.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                await LoadTopicsAsync();
                Form.Id = topicId;
                IsEditMode = true;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas aktualizacji tematu.");
                ErrorMessage = "Wyst�pi� b��d podczas aktualizacji tematu.";
                await LoadTopicsAsync();
                Form.Id = topicId;
                IsEditMode = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromForm] int topicId)
        {
            await LoadScheduleAsync();
            if (!CanEdit)
            {
                ErrorMessage = "Usuwanie temat�w jest mo�liwe tylko w trakcie aktywnej tury.";
                return RedirectToPage();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var resp = await client.DeleteAsync($"api/Topics/{topicId}");

                if (resp.IsSuccessStatusCode)
                {
                    SuccessMessage = "Temat usuni�ty.";
                    return RedirectToPage();
                }

                ErrorMessage = await ErrorTranslator.TranslateAsync(resp);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas usuwania tematu.");
                ErrorMessage = "Wyst�pi� b��d podczas usuwania tematu.";
                return RedirectToPage();
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

        private async Task LoadTopicsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                Topics = await client.GetFromJsonAsync<List<TopicDto>>("api/Topics") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas pobierania temat�w (api/Topics).");
                ErrorMessage ??= "Nie uda�o si� pobra� listy temat�w.";
            }
        }

        // ===== Forms / DTOs =====
        public class TopicForm
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public class TopicDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int PromotorId { get; set; }
        }
    }
}