using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromotorSelection.Services;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "3")]
    public class PromotorsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PromotorsModel> _logger;

        public PromotorsModel(IHttpClientFactory httpClientFactory, ILogger<PromotorsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Query params (lista)
        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "lastName"; // lastName | limit
        [BindProperty(SupportsGet = true)] public string Dir { get; set; } = "asc"; // asc | desc

        // Panel edycji/dodawania
        [BindProperty(SupportsGet = true)] public string? FormMode { get; set; } // null | create | edit
        [BindProperty(SupportsGet = true)] public int? Id { get; set; } // dla edit

        [BindProperty] public PromotorForm Form { get; set; } = new();

        public List<PromotorDto> Promotors { get; private set; } = new();

        public int TotalCount { get; private set; }
        public int FilteredCount { get; private set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadListAsync();

            if (string.Equals(FormMode, "edit", StringComparison.OrdinalIgnoreCase) && Id.HasValue)
            {
                var p = Promotors.FirstOrDefault(x => x.UserId == Id.Value);
                if (p == null)
                {
                    ErrorMessage = "Nie znaleziono promotora do edycji.";
                    FormMode = null;
                    return;
                }

                Form = new PromotorForm
                {
                    UserId = p.UserId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    StudentLimit = p.StudentLimit,
                    Password = string.Empty
                };
            }
            else if (string.Equals(FormMode, "create", StringComparison.OrdinalIgnoreCase))
            {
                Form = new PromotorForm { StudentLimit = 10 };
            }
            else
            {
                FormMode = null;
            }
        }

        public async Task<IActionResult> OnPostSaveAsync(
            [FromForm] string? formMode,
            [FromForm] string? q,
            [FromForm] string? sort,
            [FromForm] string? dir)
        {
            Q = q;
            Sort = sort ?? Sort;
            Dir = dir ?? Dir;

            formMode = (formMode ?? "").Trim().ToLowerInvariant();

            if (formMode != "create" && formMode != "edit")
            {
                ErrorMessage = "Niepoprawny tryb formularza.";
                return RedirectToPage(new { Q, Sort, Dir });
            }

            if (string.IsNullOrWhiteSpace(Form.FirstName) ||
                string.IsNullOrWhiteSpace(Form.LastName) ||
                string.IsNullOrWhiteSpace(Form.Email))
            {
                ErrorMessage = "Uzupełnij: imię, nazwisko i email.";
                return RedirectToPage(new { Q, Sort, Dir, FormMode = formMode, Id = (formMode == "edit" ? (int?)Form.UserId : null) });
            }

            if (Form.StudentLimit < 0)
            {
                ErrorMessage = "Limit nie może być ujemny.";
                return RedirectToPage(new { Q, Sort, Dir, FormMode = formMode, Id = (formMode == "edit" ? (int?)Form.UserId : null) });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                if (formMode == "create")
                {
                    if (string.IsNullOrWhiteSpace(Form.Password))
                    {
                        ErrorMessage = "Hasło jest wymagane przy dodawaniu promotora.";
                        return RedirectToPage(new { Q, Sort, Dir, FormMode = "create" });
                    }

                    // UWAGA: validator CreateUserCommand wymaga StudentLimit > 0 dla promotora
                    if (Form.StudentLimit <= 0)
                    {
                        ErrorMessage = "Limit promotora musi być większy od 0.";
                        return RedirectToPage(new { Q, Sort, Dir, FormMode = "create" });
                    }

                    var payload = new
                    {
                        firstName = Form.FirstName.Trim(),
                        lastName = Form.LastName.Trim(),
                        email = Form.Email.Trim(),
                        password = Form.Password,
                        roleId = 2,
                        albumNumber = (string?)null,
                        studentLimit = Form.StudentLimit
                    };

                    var resp = await client.PostAsJsonAsync("api/Users", payload);

                    if (resp.IsSuccessStatusCode)
                    {
                        SuccessMessage = "Dodano promotora.";
                        return RedirectToPage(new { Q, Sort, Dir });
                    }

                    ErrorMessage = ErrorTranslator.Translate(resp);

                    return RedirectToPage(new { Q, Sort, Dir, FormMode = "create" });
                }
                else // edit
                {
                    if (Form.UserId <= 0)
                    {
                        ErrorMessage = "Brak UserId do edycji.";
                        return RedirectToPage(new { Q, Sort, Dir });
                    }

                    // UpdateUserCommand dla promotora: AlbumNumber nie ma znaczenia, GradeAverage ignorowane
                    var payload = new
                    {
                        userId = Form.UserId,
                        firstName = Form.FirstName.Trim(),
                        lastName = Form.LastName.Trim(),
                        email = Form.Email.Trim(),
                        password = string.IsNullOrWhiteSpace(Form.Password) ? (string?)null : Form.Password,

                        albumNumber = (string?)null,
                        gradeAverage = (double?)null,
                        studentLimit = Form.StudentLimit
                    };

                    var resp = await client.PutAsJsonAsync($"api/Users/{Form.UserId}", payload);

                    if (resp.StatusCode == HttpStatusCode.NoContent)
                    {
                        SuccessMessage = "Zapisano zmiany.";
                        return RedirectToPage(new { Q, Sort, Dir });
                    }

                    ErrorMessage = ErrorTranslator.Translate(resp);

                    return RedirectToPage(new { Q, Sort, Dir, FormMode = "edit", Id = Form.UserId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas zapisu promotora (mode={Mode}, userId={UserId})", formMode, Form.UserId);
                ErrorMessage = "Wystąpił błąd podczas zapisu.";
                return RedirectToPage(new { Q, Sort, Dir, FormMode = formMode, Id = (formMode == "edit" ? (int?)Form.UserId : null) });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromForm] int id, [FromForm] string? q, [FromForm] string? sort, [FromForm] string? dir)
        {
            Q = q;
            Sort = sort ?? Sort;
            Dir = dir ?? Dir;

            if (id <= 0)
            {
                ErrorMessage = "Niepoprawne ID do usunięcia.";
                return RedirectToPage(new { Q, Sort, Dir });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var resp = await client.DeleteAsync($"api/Users/{id}");

                if (resp.StatusCode == HttpStatusCode.NoContent)
                {
                    SuccessMessage = "Usunięto promotora.";
                    return RedirectToPage(new { Q, Sort, Dir });
                }

                ErrorMessage = ErrorTranslator.Translate(resp);

                return RedirectToPage(new { Q, Sort, Dir });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania promotora (UserId={UserId})", id);
                ErrorMessage = "Wystąpił błąd podczas usuwania promotora.";
                return RedirectToPage(new { Q, Sort, Dir });
            }
        }

        private async Task LoadListAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var all = await client.GetFromJsonAsync<List<PromotorDto>>("api/Promotors") ?? new();

                TotalCount = all.Count;

                IEnumerable<PromotorDto> query = all;

                if (!string.IsNullOrWhiteSpace(Q))
                {
                    var q = Q.Trim();
                    bool qIsInt = int.TryParse(q, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qInt);

                    query = query.Where(p =>
                        ContainsCI(p.FirstName, q) ||
                        ContainsCI(p.LastName, q) ||
                        ContainsCI(p.Email, q) ||
                        (qIsInt && (p.UserId == qInt || p.StudentLimit == qInt))
                    );
                }

                var filtered = query.ToList();
                FilteredCount = filtered.Count;

                bool desc = string.Equals(Dir, "desc", StringComparison.OrdinalIgnoreCase);

                query = Sort switch
                {
                    "limit" => desc
                        ? filtered.OrderByDescending(p => p.StudentLimit).ThenBy(p => p.LastName)
                        : filtered.OrderBy(p => p.StudentLimit).ThenBy(p => p.LastName),

                    _ => desc
                        ? filtered.OrderByDescending(p => p.LastName).ThenBy(p => p.FirstName)
                        : filtered.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
                };

                Promotors = query.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania listy promotorów.");
                ErrorMessage = "Nie udało się pobrać promotorów z backendu.";
            }
        }

        private static bool ContainsCI(string? value, string q)
            => !string.IsNullOrWhiteSpace(value) &&
               value.Contains(q, StringComparison.OrdinalIgnoreCase);

        public string NextDir(string sort)
        {
            if (string.Equals(Sort, sort, StringComparison.OrdinalIgnoreCase))
                return string.Equals(Dir, "asc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

            return "asc";
        }

        public string SortIcon(string sort)
        {
            if (!string.Equals(Sort, sort, StringComparison.OrdinalIgnoreCase))
                return "↕";

            return string.Equals(Dir, "asc", StringComparison.OrdinalIgnoreCase) ? "↑" : "↓";
        }

        public class PromotorForm
        {
            public int UserId { get; set; } // 0 dla create

            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;

            public int StudentLimit { get; set; }

            public string? Password { get; set; }
        }

        public class PromotorDto
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int StudentLimit { get; set; }
            public List<TopicDto> Topics { get; set; } = new();
        }

        public class TopicDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int PromotorId { get; set; }
        }
    }
}