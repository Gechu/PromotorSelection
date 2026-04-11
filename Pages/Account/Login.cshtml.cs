#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace PromotorSelection.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(ILogger<LoginModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                string role = "";

                // Ustalamy rolę na podstawie wpisanego emaila
                if (Input.Email.Contains("admin")) role = "Admin";
                else if (Input.Email.Contains("promotor")) role = "Promotor";
                else if (Input.Email.Contains("student")) role = "Student";

                if (!string.IsNullOrEmpty(role))
                {
                    // 1. Tworzymy listę "tożsamości" (Claims)
                    var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, Input.Email),
                            new Claim(ClaimTypes.Role, role), // To jest kluczowe dla [Authorize(Roles = "...")]
                        };

                    var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");

                    // 2. Wystawiamy ciasteczko!
                    await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity));

                    _logger.LogInformation($"Użytkownik {Input.Email} zalogowany z rolą {role}.");

                    // 3. Przekierowanie do odpowiedniego panelu
                    if (role == "Admin") return RedirectToPage("/Admin/Index");
                    if (role == "Promotor") return RedirectToPage("/Promotor/Index");
                    if (role == "Student") return RedirectToPage("/Student/Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Nieprawidłowy login (wpisz coś z 'admin', 'promotor' lub 'student').");
                    return Page();
                }
            }

            return Page();
        }
    }
}