using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        // Query params (lista)
        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "lastName";
        [BindProperty(SupportsGet = true)] public string Dir { get; set; } = "asc";

        // Panel edycji/dodawania
        [BindProperty(SupportsGet = true)] public string? FormMode { get; set; } // null | create | edit
        [BindProperty(SupportsGet = true)] public int? Id { get; set; } // dla edit

        [BindProperty] public StudentForm Form { get; set; } = new();

        public List<StudentDto> Students { get; private set; } = new();

        public int TotalCount { get; private set; }
        public int FilteredCount { get; private set; }
        public int SoloCount { get; private set; }
        public int InTeamCount { get; private set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadListAsync();

            // Prefill formularza w trybie edit
            if (string.Equals(FormMode, "edit", StringComparison.OrdinalIgnoreCase) && Id.HasValue)
            {
                var s = Students.FirstOrDefault(x => x.UserId == Id.Value);
                if (s == null)
                {
                    ErrorMessage = "Nie znaleziono studenta do edycji.";
                    FormMode = null;
                    return;
                }

                Form = new StudentForm
                {
                    UserId = s.UserId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    AlbumNumber = s.AlbumNumber,
                    GradeAverageText = s.GradeAverage.ToString("0.00", CultureInfo.InvariantCulture),
                    Password = string.Empty
                };
            }
            else if (string.Equals(FormMode, "create", StringComparison.OrdinalIgnoreCase))
            {
                Form = new StudentForm(); // pusty
            }
            else
            {
                FormMode = null; // sanity
            }
        }

        // ===== IMPORT CSV (wywołuje backend: POST /api/Students, multipart/form-data, pole "file") =====
        public async Task<IActionResult> OnPostImportCsvAsync(
            IFormFile file,
            [FromForm] string? q,
            [FromForm] string? sort,
            [FromForm] string? dir)
        {
            Q = q;
            Sort = sort ?? Sort;
            Dir = dir ?? Dir;

            var redirect = RedirectToPage(new { Q, Sort, Dir });

            if (file == null || file.Length == 0)
            {
                ErrorMessage = "Wybierz plik CSV do importu.";
                return redirect;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                using var form = new MultipartFormDataContent();

                await using var stream = file.OpenReadStream();
                using var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "text/csv");

                // WAŻNE: nazwa pola "file" musi się zgadzać z backendem
                form.Add(fileContent, "file", file.FileName);

                var resp = await client.PostAsync("api/Students", form);

                if (resp.IsSuccessStatusCode)
                {
                    int imported = 0;
                    try
                    {
                        imported = await resp.Content.ReadFromJsonAsync<int>();
                    }
                    catch
                    {
                        // ignore (fallback)
                    }

                    SuccessMessage = imported > 0
                        ? $"Zaimportowano {imported} studentów z pliku CSV."
                        : "Import zakończony. Nie dodano nowych studentów (prawdopodobnie duplikaty email/nr albumu).";

                    return redirect;
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    var text = await resp.Content.ReadAsStringAsync();
                    ErrorMessage = string.IsNullOrWhiteSpace(text)
                        ? "Backend odrzucił import (BadRequest). Sprawdź format CSV."
                        : $"Import odrzucony: {text}";
                    return redirect;
                }

                ErrorMessage = $"Nie udało się zaimportować CSV (HTTP {(int)resp.StatusCode}).";
                return redirect;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas importu studentów z CSV.");
                ErrorMessage = "Wystąpił błąd podczas importu CSV.";
                return redirect;
            }
        }

        public async Task<IActionResult> OnPostSaveAsync(
            [FromForm] string? formMode,
            [FromForm] string? q,
            [FromForm] string? sort,
            [FromForm] string? dir)
        {
            // zachowaj query-string listy
            Q = q;
            Sort = sort ?? Sort;
            Dir = dir ?? Dir;

            formMode = (formMode ?? "").Trim().ToLowerInvariant();

            if (formMode != "create" && formMode != "edit")
            {
                ErrorMessage = "Niepoprawny tryb formularza.";
                return RedirectToPage(new { Q, Sort, Dir });
            }

            // minimalna walidacja wymaganych pól
            if (string.IsNullOrWhiteSpace(Form.FirstName) ||
                string.IsNullOrWhiteSpace(Form.LastName) ||
                string.IsNullOrWhiteSpace(Form.Email) ||
                string.IsNullOrWhiteSpace(Form.AlbumNumber))
            {
                ErrorMessage = "Uzupełnij: imię, nazwisko, email i nr albumu.";
                return RedirectToPage(new { Q, Sort, Dir, FormMode = formMode, Id = (formMode == "edit" ? (int?)Form.UserId : null) });
            }

            // GradeAverage: optional (puste = null => przy edycji backend zostawi stare)
            double? gradeAverage = null;
            if (!string.IsNullOrWhiteSpace(Form.GradeAverageText))
            {
                var raw = Form.GradeAverageText.Trim().Replace(',', '.');
                if (!double.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var g))
                {
                    ErrorMessage = "Nie udało się odczytać średniej. Wpisz np. 4.56 lub 4,56.";
                    return RedirectToPage(new { Q, Sort, Dir, FormMode = formMode, Id = (formMode == "edit" ? (int?)Form.UserId : null) });
                }

                if (g is < 2.0 or > 5.5)
                {
                    ErrorMessage = "Średnia musi być w zakresie 2.0 – 5.5.";
                    return RedirectToPage(new { Q, Sort, Dir, FormMode = formMode, Id = (formMode == "edit" ? (int?)Form.UserId : null) });
                }

                gradeAverage = g;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                if (formMode == "create")
                {
                    if (string.IsNullOrWhiteSpace(Form.Password))
                    {
                        ErrorMessage = "Hasło jest wymagane przy dodawaniu studenta.";
                        return RedirectToPage(new { Q, Sort, Dir, FormMode = "create" });
                    }

                    var payload = new
                    {
                        firstName = Form.FirstName.Trim(),
                        lastName = Form.LastName.Trim(),
                        email = Form.Email.Trim(),
                        password = Form.Password,
                        roleId = 1,
                        albumNumber = Form.AlbumNumber.Trim(),
                        studentLimit = (int?)null
                    };

                    var resp = await client.PostAsJsonAsync("api/Users", payload);

                    if (resp.IsSuccessStatusCode)
                    {
                        SuccessMessage = "Dodano studenta.";
                        return RedirectToPage(new { Q, Sort, Dir });
                    }

                    ErrorMessage = resp.StatusCode == HttpStatusCode.BadRequest
                        ? "Backend odrzucił żądanie (BadRequest). Sprawdź dane (email/nr albumu/hasło)."
                        : $"Nie udało się dodać studenta (HTTP {(int)resp.StatusCode}).";
                    return RedirectToPage(new { Q, Sort, Dir, FormMode = "create" });
                }
                else // edit
                {
                    if (Form.UserId <= 0)
                    {
                        ErrorMessage = "Brak UserId do edycji.";
                        return RedirectToPage(new { Q, Sort, Dir });
                    }

                    var payload = new
                    {
                        userId = Form.UserId,
                        firstName = Form.FirstName.Trim(),
                        lastName = Form.LastName.Trim(),
                        email = Form.Email.Trim(),
                        password = string.IsNullOrWhiteSpace(Form.Password) ? (string?)null : Form.Password,
                        albumNumber = Form.AlbumNumber.Trim(),
                        gradeAverage = gradeAverage,
                        studentLimit = (int?)null
                    };

                    var resp = await client.PutAsJsonAsync($"api/Users/{Form.UserId}", payload);

                    if (resp.StatusCode == HttpStatusCode.NoContent)
                    {
                        SuccessMessage = "Zapisano zmiany.";
                        return RedirectToPage(new { Q, Sort, Dir });
                    }

                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        ErrorMessage = "Nie znaleziono użytkownika do aktualizacji.";
                        return RedirectToPage(new { Q, Sort, Dir });
                    }

                    ErrorMessage = resp.StatusCode == HttpStatusCode.BadRequest
                        ? "Backend odrzucił żądanie (BadRequest). Sprawdź dane (np. zajęty email/nr albumu)."
                        : $"Błąd zapisu (HTTP {(int)resp.StatusCode}).";
                    return RedirectToPage(new { Q, Sort, Dir, FormMode = "edit", Id = Form.UserId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas zapisu studenta (mode={Mode}, userId={UserId})", formMode, Form.UserId);
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
                    SuccessMessage = "Usunięto studenta.";
                    return RedirectToPage(new { Q, Sort, Dir });
                }

                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Nie znaleziono użytkownika do usunięcia.";
                    return RedirectToPage(new { Q, Sort, Dir });
                }

                ErrorMessage = resp.StatusCode == HttpStatusCode.BadRequest
                    ? "Nie można usunąć studenta (np. posiada już przydział)."
                    : $"Nie udało się usunąć studenta (HTTP {(int)resp.StatusCode}).";

                return RedirectToPage(new { Q, Sort, Dir });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania studenta (UserId={UserId})", id);
                ErrorMessage = "Wystąpił błąd podczas usuwania studenta.";
                return RedirectToPage(new { Q, Sort, Dir });
            }
        }

        private async Task LoadListAsync()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var all = await client.GetFromJsonAsync<List<StudentDto>>("api/Students") ?? new();

            TotalCount = all.Count;

            IEnumerable<StudentDto> query = all;

            // FILTER
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                bool qIsInt = int.TryParse(q, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qInt);

                query = query.Where(s =>
                    ContainsCI(s.FirstName, q) ||
                    ContainsCI(s.LastName, q) ||
                    ContainsCI(s.Email, q) ||
                    ContainsCI(s.AlbumNumber, q) ||
                    (qIsInt && (s.UserId == qInt || s.TeamId == qInt))
                );
            }

            var filteredList = query.ToList();
            FilteredCount = filteredList.Count;

            SoloCount = filteredList.Count(s => s.TeamId == 0);
            InTeamCount = filteredList.Count(s => s.TeamId != 0);

            // SORT
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

        private static bool ContainsCI(string? value, string q)
            => !string.IsNullOrWhiteSpace(value) &&
               value.Contains(q, StringComparison.OrdinalIgnoreCase);

        // DTO do widoku
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

        public class StudentForm
        {
            public int UserId { get; set; } // 0 dla create

            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;

            public string AlbumNumber { get; set; } = string.Empty;

            // trzymamy jako string, żeby akceptować 4,56 i 4.56
            public string? GradeAverageText { get; set; }

            public string? Password { get; set; }
        }

        // do linków sortowania
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