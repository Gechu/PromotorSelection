using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace PromotorSelection
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //adres backendu, rejestracja httpClient
            builder.Services.AddHttpClient("BackendAPI", client =>
            {
                client.BaseAddress = new Uri("http://localhost:5005/");  // Ustawiony poprawny adres backendu
            });

            // Blokowanie dostępu dla użytkowników
            builder.Services.AddRazorPages(options =>
            {
                // Pozwól na dostęp anonimowy do strony głównej
                options.Conventions.AllowAnonymousToPage("/Index");

            });

            //ciasteczka przez api beda
            builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
             {
                 options.Cookie.Name = "MyCookieAuth";
                 options.LoginPath = "/Account/Login";
                 options.AccessDeniedPath = "/Account/AccessDenied";
             });


            var app = builder.Build();



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
    }
}
