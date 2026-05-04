using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class StudentsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public StudentsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<StudentDto> Students { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");

            Students = await client.GetFromJsonAsync<List<StudentDto>>("api/Students")
                       ?? new List<StudentDto>();
        }

        // Prowizoryczny DTO na potrzeby widoku.

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