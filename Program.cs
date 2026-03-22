using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Data;

namespace PromotorSelection
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Po³¹czenie do SQLite
            var connectionString = builder.Configuration.GetConnectionString("csConnection")
                ?? throw new InvalidOperationException("Connection string 'csConnection' not found.");

            // Rejestracja DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));

            // Identity z rolami
            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Blokowanie dostêpu dla u¿ytkowników
            builder.Services.AddRazorPages(options =>
            {
                // Pozwól na dostêp anonimowy do strony g³ównej
                options.Conventions.AllowAnonymousToPage("/Index");

                // Pozwól na dostêp anonimowy do ca³ego Identity (logowanie, rejestracja)
                options.Conventions.AllowAnonymousToFolder("/Identity");
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
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
