using Microsoft.EntityFrameworkCore;
using PromotorSelection.Infrastructure;
using PromotorSelection.Application.Students;
using PromotorSelection.Infrastructure.Interfaces;
using PromotorSelection.Infrastructure.Repositories;
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

//do studenta
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(GetStudentsQuery).Assembly);
});

builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandling>();
app.UseAuthorization();
app.MapControllers();

app.Run();