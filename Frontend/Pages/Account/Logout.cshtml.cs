#nullable disable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace PromotorSelection.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // 1) Usuń token do backendu (JWT), żeby API też przestało działać po wylogowaniu
            Response.Cookies.Delete("BackendToken");

            // 2) Wyloguj z UI (cookie auth Razor Pages)
            await HttpContext.SignOutAsync("MyCookieAuth");

            _logger.LogInformation("Użytkownik wylogowany. Usunięto MyCookieAuth oraz BackendToken.");

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToPage("/Index");
        }
    }
}