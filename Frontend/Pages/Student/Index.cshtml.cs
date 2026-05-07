using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Student
{
    [Authorize(Roles = "1")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public UserProfileDto? Profile { get; private set; }
        public ScheduleStatusDto? ScheduleStatus { get; private set; }

        public List<AlertItem> Alerts { get; private set; } = new();

        public string? ErrorMessage { get; private set; }

        public string GradeDisplay
            => Profile?.GradeAverage is null ? "—" : Profile.GradeAverage.Value.ToString("0.00");

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");

                // bezpieczny endpoint "me" 
                Profile = await client.GetFromJsonAsync<UserProfileDto>("api/Account");

                // Status tury
                ScheduleStatus = await client.GetFromJsonAsync<ScheduleStatusDto>("api/Schedules");

                BuildAlerts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania panelu studenta.");
                ErrorMessage = "Nie udało się pobrać danych z backendu do panelu studenta.";
            }
        }

        private void BuildAlerts()
        {
            Alerts = new List<AlertItem>();

            // Jeśli nie mamy profilu, nie budujemy alertów "profilowych"
            if (Profile is null)
                return;

            // Alert: tura nieaktywna
            if (ScheduleStatus is not null && !ScheduleStatus.IsActive)
            {
                Alerts.Add(AlertItem.Info(
                    title: "Tura jest nieaktywna",
                    details: "Edycja danych (średniej, zespołu, wyborów) jest możliwa tylko w wyznaczonym terminie."
                ));
            }

            // Alert: brak/nieustawiona średnia
            // (zakładamy, że null lub 0 oznacza “do uzupełnienia”)
            if (Profile.GradeAverage is null || Profile.GradeAverage <= 0.0001)
            {
                Alerts.Add(AlertItem.Warning(
                    title: "Średnia nie jest ustawiona",
                    details: "Uzupełnij lub potwierdź swoją średnią na stronie „Moja średnia”."
                ));
            }

            // Info: zespół/solo
            if (Profile.TeamId is null || Profile.TeamId == 0)
            {
                Alerts.Add(AlertItem.Info(
                    title: "Pracujesz indywidualnie (solo)",
                    details: "Jeśli planujesz projekt zespołowy, przejdź do zakładki „Zespół”."
                ));
            }
            else
            {
                Alerts.Add(AlertItem.Info(
                    title: "Jesteś w zespole",
                    details: $"Numer zespołu: {Profile.TeamId}. Pamiętaj: wyboru promotorów dokonuje lider zespołu."
                ));
            }

            // TODO: preferencje
            // Brakuje endpointu GET do pobrania aktualnych preferencji,
            // więc nie możemy pewnie stwierdzić czy student już wybrał promotorów.
            Alerts.Add(AlertItem.Info(
                title: "Sprawdź swoje wybory promotorów",
                details: "Przejdź do zakładki „Moje wybory”, aby ustawić lub edytować preferencje (jeśli tura jest aktywna)."
            ));
        }

        // ===== DTO =====

        public class UserProfileDto
        {
            public int Id { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;

            public string? AlbumNumber { get; set; }
            public double? GradeAverage { get; set; }
            public int? TeamId { get; set; }

            // Promotor fields (nieużywane na stronie studenta, ale endpoint zwraca)
            public int? StudentLimit { get; set; }
        }

        public class ScheduleStatusDto
        {
            public bool IsActive { get; set; }
            public string? Message { get; set; }
        }

        public record AlertItem(string Level, string Title, string Details)
        {
            public static AlertItem Danger(string title, string details) => new("danger", title, details);
            public static AlertItem Warning(string title, string details) => new("warning", title, details);
            public static AlertItem Info(string title, string details) => new("info", title, details);
        }
    }
}