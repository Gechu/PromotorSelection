using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "3")]
    public class StudentsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StudentsModel> _logger;

        public StudentsModel(IHttpClientFactory httpClientFactory, ILogger<StudentsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Query params
        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; } = "lastName"; // lastName | grade | team

        [BindProperty(SupportsGet = true)]
        public string Dir { get; set; } = "asc"; // asc | desc

        public List<StudentDto> Students { get; private set; } = new();

        // Statystyki dla nagłówka
        public int TotalCount { get; private set; }
        public int FilteredCount { get; private set; }
        public int SoloCount { get; private set; }
        public int InTeamCount { get; private set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");

            var all = await client.GetFromJsonAsync<List<StudentDto>>("api/Students")
                      ?? new List<StudentDto>();

            TotalCount = all.Count;

            IEnumerable<StudentDto> query = all;

            // --- FILTER ---
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();

                // jeśli wpisano liczbę, filtruj też po UserId/TeamId
                bool qIsInt = int.TryParse(q, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qInt);

                query = query.Where(s =>
                    ContainsCI(s.FirstName, q) ||
                    ContainsCI(s.LastName, q) ||
                    ContainsCI(s.Email, q) ||
                    ContainsCI(s.AlbumNumber, q) ||
                    (qIsInt && (s.UserId == qInt || s.TeamId == qInt))
                );
            }

            // --- STATS on filtered ---
            var filteredList = query.ToList();
            FilteredCount = filteredList.Count;

            // TeamId: 0 - brak zespołu, >0 - id zespołu
            SoloCount = filteredList.Count(s => s.TeamId == 0);
            InTeamCount = filteredList.Count(s => s.TeamId != 0);

            // --- SORT ---
            bool desc = string.Equals(Dir, "desc", StringComparison.OrdinalIgnoreCase);

            query = Sort switch
            {
                "grade" => desc
                    ? filteredList.OrderByDescending(s => s.GradeAverage).ThenBy(s => s.LastName)
                    : filteredList.OrderBy(s => s.GradeAverage).ThenBy(s => s.LastName),

                "team" => desc
                    ? filteredList.OrderByDescending(s => s.TeamId).ThenBy(s => s.LastName)
                    : filteredList.OrderBy(s => s.TeamId).ThenBy(s => s.LastName),

                _ => desc
                    ? filteredList.OrderByDescending(s => s.LastName).ThenBy(s => s.FirstName)
                    : filteredList.OrderBy(s => s.LastName).ThenBy(s => s.FirstName),
            };

            Students = query.ToList();
        }

        public async Task<IActionResult> OnPostUpdateGradeAsync([FromForm] UpdateGradeForm form)
        {
            // zachowaj query-string po zapisie (żeby nie resetowało filtra/sortowania)
            var redirect = RedirectToPage(new { Q, Sort, Dir });

            // Minimalna walidacja wymaganych pól (UpdateUserCommand ich wymaga)
            if (form.UserId <= 0 ||
                string.IsNullOrWhiteSpace(form.FirstName) ||
                string.IsNullOrWhiteSpace(form.LastName) ||
                string.IsNullOrWhiteSpace(form.Email))
            {
                ErrorMessage = "Niepoprawne dane formularza.";
                return redirect;
            }

            // Ręczne parsowanie średniej (akceptuj 4,56 i 4.56)
            var raw = (form.NewGrade ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                ErrorMessage = "Podaj wartość średniej.";
                return redirect;
            }

            raw = raw.Replace(',', '.');

            if (!double.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var newGrade))
            {
                ErrorMessage = "Nie udało się odczytać średniej. Wpisz np. 4.56 lub 4,56.";
                return redirect;
            }

            // walidacja jak w backendzie (UpdateGradeCommandValidator 2.0-5.5)
            if (newGrade is < 2.0 or > 5.5)
            {
                ErrorMessage = "Średnia musi być w zakresie 2.0 – 5.5.";
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
                    albumNumber = form.AlbumNumber,
                    gradeAverage = newGrade,     // <- tu idzie już sparsowany double
                    studentLimit = (int?)null
                };

                var resp = await client.PutAsJsonAsync($"api/Users/{form.UserId}", payload);

                if (resp.StatusCode == HttpStatusCode.NoContent)
                {
                    SuccessMessage = "Zapisano nową średnią.";
                    return redirect;
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    ErrorMessage = "Backend odrzucił żądanie (BadRequest). Sprawdź dane.";
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
                _logger.LogError(ex, "Błąd podczas aktualizacji średniej studenta (UserId={UserId})", form.UserId);
                ErrorMessage = "Wystąpił błąd podczas zapisu średniej.";
                return redirect;
            }
        }

        private static bool ContainsCI(string? value, string q)
            => !string.IsNullOrWhiteSpace(value) &&
               value.Contains(q, StringComparison.OrdinalIgnoreCase);

        // DTO na potrzeby widoku
        public class StudentDto
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string AlbumNumber { get; set; } = string.Empty;
            public double GradeAverage { get; set; }
            public int TeamId { get; set; }
        }

        public class UpdateGradeForm
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string AlbumNumber { get; set; } = string.Empty;
            public string? NewGrade { get; set; }
        }

        // Pomocnicze: do generowania linków sortowania w widoku
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
    }
}