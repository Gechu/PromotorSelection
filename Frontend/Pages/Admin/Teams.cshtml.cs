using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "3")]
    public class TeamsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TeamsModel> _logger;

        public TeamsModel(IHttpClientFactory httpClientFactory, ILogger<TeamsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Query params
        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; } = "id"; // id | members

        [BindProperty(SupportsGet = true)]
        public string Dir { get; set; } = "asc"; // asc | desc

        public List<TeamRow> Teams { get; private set; } = new();

        public int TotalCount { get; private set; }
        public int FilteredCount { get; private set; }

        public string? ErrorMessage { get; private set; }

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Pobieramy teamy + studentów, żeby wzbogacić członków o email/album/średnią
                var allTeams = await client.GetFromJsonAsync<List<TeamDto>>("api/Teams") ?? new();
                var students = await client.GetFromJsonAsync<List<StudentDto>>("api/Students") ?? new();

                var studentsById = students
                    .GroupBy(s => s.UserId)
                    .ToDictionary(g => g.Key, g => g.First());

                TotalCount = allTeams.Count;

                // Mapowanie do modelu widoku
                var mapped = allTeams.Select(t =>
                {
                    var members = t.Members.Select(m =>
                    {
                        studentsById.TryGetValue(m.UserId, out var s);

                        return new TeamMemberRow
                        {
                            UserId = m.UserId,
                            FirstName = m.FirstName,
                            LastName = m.LastName,
                            Email = s?.Email,
                            AlbumNumber = s?.AlbumNumber,
                            GradeAverage = s?.GradeAverage
                        };
                    }).ToList();

                    var leader = members.FirstOrDefault(x => x.UserId == t.LeaderId);

                    return new TeamRow
                    {
                        Id = t.Id,
                        TeamSize = t.TeamSize,
                        IsClosed = t.TeamSize == -1,
                        LeaderId = t.LeaderId,
                        CurrentMembersCount = t.CurrentMembersCount,
                        LeaderDisplayName = leader != null
                            ? $"{leader.LastName} {leader.FirstName}"
                            : $"{t.LeaderId}", // fallback - raczej się nie zdarzy
                        Members = members
                    };
                }).ToList();

                IEnumerable<TeamRow> query = mapped;

                // FILTER (po ID/leaderId/liczbie członków oraz po danych członków)
                if (!string.IsNullOrWhiteSpace(Q))
                {
                    var q = Q.Trim();
                    bool qIsInt = int.TryParse(q, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qInt);

                    query = query.Where(t =>
                        (qIsInt && (
                            t.Id == qInt ||
                            t.LeaderId == qInt ||
                            t.TeamSize == qInt ||
                            t.CurrentMembersCount == qInt
                        )) ||
                        ContainsCI(t.LeaderDisplayName, q) ||
                        t.Members.Any(m =>
                            ContainsCI(m.FirstName, q) ||
                            ContainsCI(m.LastName, q) ||
                            ContainsCI(m.Email, q) ||
                            ContainsCI(m.AlbumNumber, q)
                        )
                    );
                }

                var filtered = query.ToList();
                FilteredCount = filtered.Count;

                // SORT
                bool desc = string.Equals(Dir, "desc", StringComparison.OrdinalIgnoreCase);

                query = Sort switch
                {
                    "members" => desc
                        ? filtered.OrderByDescending(t => t.CurrentMembersCount).ThenBy(t => t.Id)
                        : filtered.OrderBy(t => t.CurrentMembersCount).ThenBy(t => t.Id),

                    _ => desc
                        ? filtered.OrderByDescending(t => t.Id)
                        : filtered.OrderBy(t => t.Id)
                };

                Teams = query.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania listy zespołów.");
                ErrorMessage = "Nie udało się pobrać zespołów z backendu.";
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

        // ===== Modele dla widoku =====

        public class TeamRow
        {
            public int Id { get; set; }
            public int TeamSize { get; set; } // -1 oznacza zamknięty
            public bool IsClosed { get; set; }

            public int LeaderId { get; set; }
            public string LeaderDisplayName { get; set; } = string.Empty;

            public int CurrentMembersCount { get; set; }
            public List<TeamMemberRow> Members { get; set; } = new();
        }

        public class TeamMemberRow
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;

            public string? Email { get; set; }
            public string? AlbumNumber { get; set; }
            public double? GradeAverage { get; set; }
        }

        // ===== DTO do API =====

        public class TeamDto
        {
            public int Id { get; set; }
            public int TeamSize { get; set; }
            public int LeaderId { get; set; }
            public int CurrentMembersCount { get; set; }
            public List<TeamMemberDto> Members { get; set; } = new();
        }

        public class TeamMemberDto
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
        }

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
    }
}