using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Users;

public record ImportStudentsCommand(Stream FileStream) : IRequest<int>;

public class ImportStudentsHandler : IRequestHandler<ImportStudentsCommand, int>
{
    private readonly IApplicationDbContext _context;

    public ImportStudentsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(ImportStudentsCommand request, CancellationToken ct)
    {
        var studentsToImport = new List<StudentCsvRecord>();

        try
        {
            using var reader = new StreamReader(request.FileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ","
            });

            studentsToImport = csv.GetRecords<StudentCsvRecord>().ToList();
        }
        catch (Exception ex)
        {
            throw new BadRequestException($"Błąd podczas odczytu pliku CSV: {ex.Message}");
        }

        if (!studentsToImport.Any())
            throw new BadRequestException("Plik CSV nie zawiera danych.");

        int importedCount = 0;
        await _context.BeginTransactionAsync(ct);

        try
        {
            foreach (var record in studentsToImport)
            {
                
                if (await _context.Users.AnyAsync(u => u.Email == record.Email, ct)) continue;
                if (await _context.Students.AnyAsync(s => s.AlbumNumber == record.AlbumNumber, ct)) continue;

                var user = new User
                {
                    FirstName = record.FirstName,
                    LastName = record.LastName,
                    Email = record.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(record.AlbumNumber),
                    RoleId = 1
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(ct);

                var student = new Student
                {
                    UserId = user.Id,
                    AlbumNumber = record.AlbumNumber,
                    GradeAverage = record.GradeAverage
                };

                _context.Students.Add(student);
                importedCount++;
            }

            await _context.SaveChangesAsync(ct);
            await _context.CommitTransactionAsync(ct);
        }
        catch (Exception)
        {
            await _context.RollbackTransactionAsync(ct);
            throw;
        }

        return importedCount;
    }
}

public class StudentCsvRecord
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AlbumNumber { get; set; } = string.Empty;
    public double? GradeAverage { get; set; }
}