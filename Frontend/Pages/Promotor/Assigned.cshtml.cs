using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Promotor
{
    [Authorize(Roles = "Promotor")]
    public class AssignedModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
