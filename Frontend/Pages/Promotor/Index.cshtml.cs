using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Promotor
{
    [Authorize(Roles = "2")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ScheduleStatusDto? ScheduleStatus { get; private set; }

        public int StudentLimit { get; private set; }
        public int? InterestedCount { get; private set; }
        public int? FreeSlots { get; private set; }
        public int AssignedCount { get; private set; }

        public string? TimeToEndDisplay
        {
            get
            {
                if (ScheduleStatus?.EndDate is null) return null;

                var now = DateTime.Now;
                var end = ScheduleStatus.EndDate.Value;

                if (now >= end) return "0m";

                var diff = end - now;

                if (diff.TotalDays >= 1)
                    return $"{(int)diff.TotalDays}d {diff.Hours}h";

                if (diff.TotalHours >= 1)
                    return $"{(int)diff.TotalHours}h {diff.Minutes}m";

                return $"{diff.Minutes}m";
            }
        }

        public string? ErrorMessage { get; private set; }

        public async Task OnGetAsync()
        {
            try
            {
                await LoadScheduleAsync();
                await LoadPromotorDataAsync();
                await LoadStatisticsAsync();
                await LoadAssignedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania danych panelu promotora.");
                ErrorMessage = "Nie uda³o siê pobraæ wszystkich danych. Spróbuj odœwie¿yæ stronê.";
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
                _logger.LogError(ex, "B³¹d podczas pobierania statusu tury (api/Schedules).");
            }
        }

        private async Task LoadPromotorDataAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var promotors = await client.GetFromJsonAsync<List<PromotorDto>>("api/Promotors");

                if (promotors == null || promotors.Count == 0)
                    return;

                // Szukamy zalogowanego promotora — niestety nie mamy "mnie" bezpoœrednio,
                // wiêc bêdziemy polegaæ na statystykach gdzie ID bêdzie znane.
                // Na razie bierzemy limit z GetCurrentUser (jeœli dostêpne) albo z pierwszego promotora.
                // 
                // UWAGA: To jest nieidealne — idealnie by³oby mieæ endpoint "GET /api/Promotors/me"
                // Na razie u¿ywamy: brak, bêdziemy liczyæ z Statistics
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania danych promotora (api/Promotors).");
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var stats = await client.GetFromJsonAsync<StatisticsDto>("api/Statistics");

                if (stats?.PromotorOccupancy is null || stats.PromotorOccupancy.Count == 0)
                    return;

                // Wyci¹gamy z PromotorOccupancy zalogowanego promotora.
                // WA¯NE: musimy wiedzieæ jakie jest ID zalogowanego promotora.
                // 
                // Problemem jest ¿e z JWT Token mo¿emy wyci¹gn¹æ sub (UserId) jako int.
                // Zak³adamy ¿e User.FindFirst(ClaimTypes.NameIdentifier) daje nam UserId.

                var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(currentUserIdStr, out var currentUserId))
                {
                    _logger.LogWarning("Nie uda³o siê ustaliæ ID zalogowanego promocora z claims.");
                    return;
                }

                var myOccupancy = stats.PromotorOccupancy
                    .FirstOrDefault(p => p.PromotorId == currentUserId);

                if (myOccupancy is null)
                    return;

                StudentLimit = myOccupancy.StudentLimit;
                InterestedCount = myOccupancy.InterestedStudentsCount;
                FreeSlots = Math.Max(0, StudentLimit - myOccupancy.InterestedStudentsCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania statystyk (api/Statistics).");
            }
        }

        private async Task LoadAssignedAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var assigned = await client.GetFromJsonAsync<List<AllocationResultDto>>("api/Statistics/promotor-allocations");

                AssignedCount = assigned?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania przydzielonych (api/Statistics/promotor-allocations).");
                // To nie blokuje strony — jeœli przidzia³ nie by³, to naturalnie bêdzie 0
                AssignedCount = 0;
            }
        }

        // ===== DTOs =====
        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public class PromotorDto
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int StudentLimit { get; set; }
        }

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

        public class AllocationResultDto
        {
            public int StudentId { get; set; }
            public string StudentFirstName { get; set; } = string.Empty;
            public string StudentLastName { get; set; } = string.Empty;
            public string AlbumNumber { get; set; } = string.Empty;
            public double? GradeAverage { get; set; }
            public int PromotorId { get; set; }
            public string PromotorFirstName { get; set; } = string.Empty;
            public string PromotorLastName { get; set; } = string.Empty;
        }
    }
}