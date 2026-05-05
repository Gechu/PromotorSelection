using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Student
{
    [Authorize(Roles = "1")]
    public class PromotorsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
