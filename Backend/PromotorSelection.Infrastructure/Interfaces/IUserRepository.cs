using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Infrastructure.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task AddAsync(User user);
    Task AddStudentAsync(Student student);
    Task AddPromotorAsync(Promotor promotor);
    void Delete(User user);
    Task SaveChangesAsync();
}