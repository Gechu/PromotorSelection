using System.Globalization;
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

        public StudentsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

        // Pomocnicze: do generowania linków sortowania w widoku
        public string NextDir(string sort)
        {
            // jeśli klikamy tę samą kolumnę — przełącz kierunek, w innym wypadku asc
            if (string.Equals(Sort, sort, StringComparison.OrdinalIgnoreCase))
                return string.Equals(Dir, "asc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

            return "asc";
        }

        public string SortIcon(string sort)
        {
            // jeśli to nie jest aktualnie sortowana kolumna: pokaż “↕”
            if (!string.Equals(Sort, sort, StringComparison.OrdinalIgnoreCase))
                return "↕";

            // jeśli jest sortowana: pokaż kierunek
            return string.Equals(Dir, "asc", StringComparison.OrdinalIgnoreCase) ? "↑" : "↓";
        }
    }
}