#nullable disable
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PromotorSelection.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginModel(ILogger<LoginModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }
        }

        private sealed class LoginResponse
        {
            public string Token { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            // 1) Logowanie do BACKENDU (JWT)
            var api = _httpClientFactory.CreateClient("BackendAPI");

            var resp = await api.PostAsJsonAsync("api/Auth/login", new
            {
                email = Input.Email,
                password = Input.Password
            });

            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Nieprawidłowy e-mail lub hasło.");
                return Page();
            }

            var payload = await resp.Content.ReadFromJsonAsync<LoginResponse>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
            {
                ModelState.AddModelError(string.Empty, "Backend nie zwrócił tokena.");
                return Page();
            }

            // 2) Zapis tokena w HttpOnly cookie (do wywołań API przez HttpClient)
            Response.Cookies.Append("BackendToken", payload.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // local dev; na produkcji true + HTTPS
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            // 3) Dekoduj JWT i wyciągnij rolę ("1" student, "2" promotor, "3" admin)
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.Token);

            var role =
                jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError(string.Empty, "Token nie zawiera informacji o roli.");
                return Page();
            }

            // (opcjonalnie) możesz też wyciągnąć userId, gdybyś chciał go trzymać w cookie UI
            var userId =
                jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;

            // 4) Cookie sesyjne dla Razor Pages (UI) - już z rolą zgodną z backendem
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Input.Email),
                new Claim(ClaimTypes.Role, role),
            };

            if (!string.IsNullOrWhiteSpace(userId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));

            _logger.LogInformation("Zalogowano. Email={Email}, RoleId={RoleId}", Input.Email, role);

            // 5) Przekierowanie wg roli (RoleId)
            return role switch
            {
                "3" => RedirectToPage("/Admin/Index"),
                "2" => RedirectToPage("/Promotor/Index"),
                "1" => RedirectToPage("/Student/Index"),
                _ => LocalRedirect(returnUrl),
            };
        }
    }
}