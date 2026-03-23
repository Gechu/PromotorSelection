using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class StudentsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
