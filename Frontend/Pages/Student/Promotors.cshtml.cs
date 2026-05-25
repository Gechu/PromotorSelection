using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Student
{
    [Authorize(Roles = "1")]
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
        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string Sort { get; set; } = "lastName"; // lastName | limit | interested | free

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string Dir { get; set; } = "asc"; // asc | desc

        public List<PromotorRow> Promotors { get; private set; } = new();

        public int TotalCount { get; private set; }
        public int FilteredCount { get; private set; }

        public string? ErrorMessage { get; private set; }

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // 1) promotorzy + tematy + limit
                var promotors = await client.GetFromJsonAsync<List<PromotorDto>>("api/Promotors") ?? new();

                // 2) statystyki 
                StatisticsDto? stats = null;
                try
                {
                    stats = await client.GetFromJsonAsync<StatisticsDto>("api/Statistics");
                }
                catch
                {
                    // jeśli statystyki padną, strona dalej działa, tylko bez zainteresowanych/wolnych
                }

                var occupancyById = (stats?.PromotorOccupancy ?? new List<PromotorOccupancyDto>())
                    .GroupBy(x => x.PromotorId)
                    .ToDictionary(g => g.Key, g => g.First());

                var mapped = promotors.Select(p =>
                {
                    occupancyById.TryGetValue(p.UserId, out var occ);

                    // preferujemy limit ze statystyk (żeby spójnie liczyć wolne),
                    // ale jeśli brak statystyk, zostaje ten z /api/Promotors
                    var limit = occ?.StudentLimit ?? p.StudentLimit;

                    int? interested = occ?.InterestedStudentsCount;
                    int? free = interested.HasValue ? Math.Max(0, limit - interested.Value) : (int?)null;

                    return new PromotorRow
                    {
                        UserId = p.UserId,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        Email = p.Email,
                        StudentLimit = limit,
                        InterestedStudentsCount = interested,
                        FreeSlots = free,
                        Topics = p.Topics ?? new List<TopicDto>()
                    };
                }).ToList();

                TotalCount = mapped.Count;

                IEnumerable<PromotorRow> query = mapped;

                // FILTER
                if (!string.IsNullOrWhiteSpace(Q))
                {
                    var q = Q.Trim();
                    query = query.Where(p =>
                        ContainsCI(p.FirstName, q) ||
                        ContainsCI(p.LastName, q) ||
                        ContainsCI(p.Email, q));
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

                    "interested" => desc
                        ? filtered.OrderByDescending(p => p.InterestedStudentsCount ?? -1).ThenBy(p => p.LastName)
                        : filtered.OrderBy(p => p.InterestedStudentsCount ?? int.MaxValue).ThenBy(p => p.LastName),

                    "free" => desc
                        ? filtered.OrderByDescending(p => p.FreeSlots ?? -1).ThenBy(p => p.LastName)
                        : filtered.OrderBy(p => p.FreeSlots ?? int.MaxValue).ThenBy(p => p.LastName),

                    _ => desc
                        ? filtered.OrderByDescending(p => p.LastName).ThenBy(p => p.FirstName)
                        : filtered.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
                };

                Promotors = query.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania listy promotorów dla studenta.");
                ErrorMessage = "Nie udało się pobrać listy promotorów z backendu.";
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

        // ===== View model =====
        public class PromotorRow
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;

            public int StudentLimit { get; set; }

            public int? InterestedStudentsCount { get; set; }
            public int? FreeSlots { get; set; }

            public List<TopicDto> Topics { get; set; } = new();
        }

        // ===== API DTOs =====
        public class PromotorDto
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int StudentLimit { get; set; }
            public List<TopicDto>? Topics { get; set; }
        }

        public class TopicDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int PromotorId { get; set; }
        }

        // DTO dla /api/Statistics
        public class StatisticsDto
        {
            public int TotalTeams { get; set; }
            public int FreelancersCount { get; set; }
            public int IdleStudentsCount { get; set; }
            public List<PromotorOccupancyDto> PromotorOccupancy { get; set; } = new();
        }

        public class PromotorOccupancyDto
        {
            public int PromotorId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public int InterestedStudentsCount { get; set; }
            public int StudentLimit { get; set; }
        }
    }
}