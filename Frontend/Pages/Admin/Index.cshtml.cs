using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "3")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public int StudentsCount { get; private set; }
        public int PromotorsCount { get; private set; }
        public int TeamsCount { get; private set; }
        public int UnassignedStudentsCount { get; private set; }

        public string? ErrorMessage { get; private set; }

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // 1) Students
                var students = await client.GetFromJsonAsync<List<StudentDto>>("api/Students")
                               ?? new List<StudentDto>();
                StudentsCount = students.Count;
                UnassignedStudentsCount = students.Count(s => s.TeamId == 0);

                // 2) Promotors
                var promotors = await client.GetFromJsonAsync<List<PromotorDto>>("api/Promotors")
                                ?? new List<PromotorDto>();
                PromotorsCount = promotors.Count;

                // 3) Teams (w backendzie kontroler ma [Authorize(Roles="1,3")])
                var teams = await client.GetFromJsonAsync<List<TeamDto>>("api/Teams")
                            ?? new List<TeamDto>();
                TeamsCount = teams.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania dashboardu admina.");
                ErrorMessage = "Nie uda³o siê pobraæ danych z backendu do dashboardu admina. SprawdŸ czy backend dzia³a oraz czy autoryzacja (role) jest spójna.";
            }
        }

        // Minimalne DTO: trzymamy tylko pola potrzebne do statystyk.
        // Je¿eli kontrakt API ma inne nazwy, dopasuj je.
        public class StudentDto
        {
            public int TeamId { get; set; }
        }

        public class PromotorDto
        {
            public int Id { get; set; }
        }

        public class TeamDto
        {
            public int Id { get; set; }
        }
    }
}