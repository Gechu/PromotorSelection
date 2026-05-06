using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        // Query params
        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; } = "lastName"; // lastName | limit

        [BindProperty(SupportsGet = true)]
        public string Dir { get; set; } = "asc"; // asc | desc

        public List<PromotorDto> Promotors { get; private set; } = new();

        public int TotalCount { get; private set; }
        public int FilteredCount { get; private set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var all = await client.GetFromJsonAsync<List<PromotorDto>>("api/Promotors") ?? new();

                TotalCount = all.Count;

                IEnumerable<PromotorDto> query = all;

                // FILTER
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

                // SORT
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

        public async Task<IActionResult> OnPostUpdateLimitAsync([FromForm] UpdateLimitForm form)
        {
            var redirect = RedirectToPage(new { Q, Sort, Dir });

            // Minimalna walidacja (UpdateUserCommand wymaga tych pól)
            if (form.UserId <= 0 ||
                string.IsNullOrWhiteSpace(form.FirstName) ||
                string.IsNullOrWhiteSpace(form.LastName) ||
                string.IsNullOrWhiteSpace(form.Email))
            {
                ErrorMessage = "Niepoprawne dane formularza.";
                return redirect;
            }

            if (form.NewLimit < 0)
            {
                ErrorMessage = "Limit nie może być ujemny.";
                return redirect;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");


                var payload = new
                {
                    userId = form.UserId,
                    firstName = form.FirstName,
                    lastName = form.LastName,
                    email = form.Email,
                    password = (string?)null,

                    // obejście walidatora UpdateUserCommand:
                    // AlbumNumber nie może być puste, mimo że edytujemy promotora
                    albumNumber = "N/A",

                    gradeAverage = (double?)null,
                    studentLimit = form.NewLimit
                };

                var resp = await client.PutAsJsonAsync($"api/Users/{form.UserId}", payload);

                if (resp.StatusCode == HttpStatusCode.NoContent)
                {
                    SuccessMessage = "Zapisano nowy limit.";
                    return redirect;
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    var tekst = await resp.Content.ReadAsStringAsync();
                    ErrorMessage = string.IsNullOrWhiteSpace(tekst)
                        ? "Backend odrzucił żądanie (BadRequest)."
                        : $"BadRequest: {tekst}";
                    return redirect;
                }

                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Nie znaleziono użytkownika do aktualizacji.";
                    return redirect;
                }

                var text = await resp.Content.ReadAsStringAsync();
                ErrorMessage = $"Błąd zapisu (HTTP {(int)resp.StatusCode}). {text}";
                return redirect;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji limitu promotora (UserId={UserId})", form.UserId);
                ErrorMessage = "Wystąpił błąd podczas zapisu limitu.";
                return redirect;
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

        // DTO do widoku (dopasowany do backendowego PromotorDto + TopicDto)
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

        public class UpdateLimitForm
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int NewLimit { get; set; }
        }
    }
}