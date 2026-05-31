using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Promotor
{
    [Authorize(Roles = "2")]
    public class AssignedModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AssignedModel> _logger;

        public AssignedModel(IHttpClientFactory httpClientFactory, ILogger<AssignedModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<AllocationResultDto> AssignedStudents { get; private set; } = new();

        public bool AllocationExecuted
        {
            get => AssignedStudents.Count > 0 || HasCheckedAllocation;
        }

        private bool HasCheckedAllocation { get; set; } = false;

        public string? ErrorMessage { get; private set; }

        public async Task OnGetAsync()
        {
            await LoadAssignedStudentsAsync();
        }

        private async Task LoadAssignedStudentsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var assigned = await client.GetFromJsonAsync<List<AllocationResultDto>>("api/Statistics/promotor-allocations");

                if (assigned is not null)
                {
                    AssignedStudents = assigned
                        .OrderByDescending(s => s.GradeAverage ?? 0)
                        .ToList();
                    HasCheckedAllocation = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania przydzielonych studentów (api/Statistics/promotor-allocations).");
                ErrorMessage = "Nie uda³o siê pobraæ listy przydzielonych studentów.";
                HasCheckedAllocation = false;
            }
        }

        // ===== DTOs =====
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