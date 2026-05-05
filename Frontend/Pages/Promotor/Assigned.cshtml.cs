using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Promotor
{
    [Authorize(Roles = "2")]
    public class AssignedModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
