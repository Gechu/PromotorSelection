#nullable disable

using System;
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
            // Zamiast SignInManager, czyścimy ciasteczko bezpośrednio w HttpContext
            // "MyCookieAuth" musi być takie samo jak nazwa podana w Program.cs
            await HttpContext.SignOutAsync("MyCookieAuth");

            _logger.LogInformation("Użytkownik wylogowany (czyszczenie ciasteczka).");

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage("/Index");
            }
        }
    }
}