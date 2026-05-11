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
        public int TopicsCount { get; private set; }

        public List<AlertItem> Alerts { get; private set; } = new();
        public ScheduleStatusDto? ScheduleStatus { get; private set; }

        public string? ErrorMessage { get; private set; }

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

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // Dane podstawowe
                var students = await client.GetFromJsonAsync<List<StudentDto>>("api/Students") ?? new();
                var promotors = await client.GetFromJsonAsync<List<PromotorDto>>("api/Promotors") ?? new();
                var teams = await client.GetFromJsonAsync<List<TeamDto>>("api/Teams") ?? new();

                StudentsCount = students.Count;
                PromotorsCount = promotors.Count;
                TeamsCount = teams.Count;

                // Tematy liczymy z /api/Promotors (bo /api/Topics zwraca tylko tematy aktualnie zalogowanego promotora)
                TopicsCount = promotors.Sum(p => p.Topics?.Count ?? 0);

                // Status harmonogramu/systemu (API: GET api/Schedules)
                ScheduleStatus = await client.GetFromJsonAsync<ScheduleStatusDto>("api/Schedules");

                BuildAlerts(students, promotors, TopicsCount, ScheduleStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania panelu administratora.");
                ErrorMessage = "Nie uda³o siê pobraæ danych z backendu do panelu administratora.";
            }
        }

        private void BuildAlerts(
            List<StudentDto> students,
            List<PromotorDto> promotors,
            int topicsCount,
            ScheduleStatusDto? schedule)
        {
            Alerts = new List<AlertItem>();

            // Alert: brak harmonogramu
            if (schedule is null || schedule.StartDate is null || schedule.EndDate is null)
            {
                Alerts.Add(AlertItem.Warning(
                    title: "Brak ustawionego harmonogramu",
                    details: "Ustaw datê rozpoczêcia i zakoñczenia wyborów w zak³adce „Harmonogram”."
                ));
            }
            else
            {
                // Alert: daty niepoprawne (defensywnie)
                if (schedule.StartDate >= schedule.EndDate)
                {
                    Alerts.Add(AlertItem.Danger(
                        title: "Niepoprawny harmonogram",
                        details: "Data rozpoczêcia jest póŸniejsza lub równa dacie zakoñczenia."
                    ));
                }
            }

            // Alert 1: brak studentów
            if (students.Count == 0)
            {
                Alerts.Add(AlertItem.Danger(
                    title: "Brak studentów w systemie",
                    details: "Zaimportuj studentów lub dodaj ich rêcznie, inaczej wybory nie rusz¹."
                ));
            }

            // Alert 2: brak promotorów
            if (promotors.Count == 0)
            {
                Alerts.Add(AlertItem.Danger(
                    title: "Brak promotorów w systemie",
                    details: "Dodaj promotorów, inaczej studenci nie bêd¹ mieli kogo wybraæ."
                ));
            }

            // Alert 3: brak tematów
            if (topicsCount == 0)
            {
                Alerts.Add(AlertItem.Warning(
                    title: "Brak tematów",
                    details: "Promotorzy nie dodali jeszcze tematów — studenci nie bêd¹ mieli czego wybieraæ."
                ));
            }

            // Alert 4: za ma³o miejsc u promotorów vs liczba studentów
            var totalSeats = promotors.Sum(p => p.StudentLimit ?? 0);
            if (promotors.Count > 0 && students.Count > 0 && totalSeats > 0 && totalSeats < students.Count)
            {
                Alerts.Add(AlertItem.Warning(
                    title: "Za ma³o miejsc u promotorów",
                    details: $"Suma limitów promotorów ({totalSeats}) jest mniejsza ni¿ liczba studentów ({students.Count})."
                ));
            }

            if (promotors.Count > 0 && totalSeats == 0)
            {
                Alerts.Add(AlertItem.Info(
                    title: "Limity promotorów wygl¹daj¹ na nieustawione",
                    details: "Suma limitów = 0. SprawdŸ czy endpoint /api/Promotors zwraca StudentLimit lub czy limity s¹ ustawione."
                ));
            }
        }

        // ===== DTO minimalne do statystyk/alertów =====

        public class StudentDto
        {
            public int UserId { get; set; }
        }

        public class PromotorDto
        {
            public int UserId { get; set; }
            public int? StudentLimit { get; set; }
            public List<TopicDto> Topics { get; set; } = new();
        }

        public class TeamDto
        {
            public int Id { get; set; }
        }

        public class TopicDto
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public int PromotorId { get; set; }
        }

        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }

            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public record AlertItem(string Level, string Title, string Details)
        {
            public static AlertItem Danger(string title, string details) => new("danger", title, details);
            public static AlertItem Warning(string title, string details) => new("warning", title, details);
            public static AlertItem Info(string title, string details) => new("info", title, details);
        }
    }
}