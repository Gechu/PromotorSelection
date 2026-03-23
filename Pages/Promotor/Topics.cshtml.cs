using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Promotor
{
    [Authorize(Roles = "Promotor")]
    public class TopicsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
