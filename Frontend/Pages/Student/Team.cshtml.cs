using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Student
{
    [Authorize(Roles = "Student")]
    public class TeamModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
