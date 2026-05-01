using Microsoft.EntityFrameworkCore;
using PromotorSelection.Domain.Entities;
using PromotorSelection.Infrastructure.Interfaces;

namespace PromotorSelection.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    public UserRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<User>> GetAllAsync() => await _context.Users.ToListAsync();
    public async Task<User?> GetByIdAsync(int id) => await _context.Users.FindAsync(id);

    public async Task AddAsync(User user) => await _context.Users.AddAsync(user);
    public async Task AddStudentAsync(Student student) => await _context.Students.AddAsync(student);

    public async Task AddPromotorAsync(Promotor promotor) => await _context.Promotors.AddAsync(promotor);
    public void Delete(User user) => _context.Users.Remove(user);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}