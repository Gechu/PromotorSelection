using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Data;

namespace PromotorSelection
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Połączenie do SQLite
            var connectionString = builder.Configuration.GetConnectionString("csConnection")
                ?? throw new InvalidOperationException("Connection string 'csConnection' not found.");

            // Rejestracja DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));

            // Identity z rolami
            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Blokowanie dostępu dla użytkowników
            builder.Services.AddRazorPages(options =>
            {
                // Pozwól na dostęp anonimowy do strony głównej
                options.Conventions.AllowAnonymousToPage("/Index");

                // Pozwól na dostęp anonimowy do całego Identity (logowanie, rejestracja)
                options.Conventions.AllowAnonymousToFolder("/Identity");
            });


            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.SlidingExpiration = false;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";

                // Wymusza wylogowanie po restarcie aplikacji
                options.Cookie.Name = "AuthCookie_" + Guid.NewGuid().ToString();
            });



            var app = builder.Build();

            // Tworzenie ról przy starcie aplikacji
            await CreateRolesAsync(app);

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }

        private static async Task CreateRolesAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Student", "Promotor", "Admin" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
