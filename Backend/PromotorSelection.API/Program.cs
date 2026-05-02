using Microsoft.EntityFrameworkCore;
using PromotorSelection.Infrastructure;
using PromotorSelection.Application.Students;

using PromotorSelection.Application.Common.Behaviors;
using PromotorSelection.API.Middleware;
using FluentValidation;
using System.Reflection;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>options.UseSqlite(connectionString,b => b.MigrationsAssembly("PromotorSelection.Infrastructure")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(PromotorSelection.Application.Mappings.MappingProfile).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(GetStudentsQuery).Assembly);
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(GetStudentsQuery).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandling>();
app.UseAuthorization();
app.MapControllers();

app.Run();