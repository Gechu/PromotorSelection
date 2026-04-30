using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Infrastructure.Interfaces
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllAsync();
        Task<Student?> GetByIdAsync(int id);
        Task AddAsync(Student student);
        Task SaveChangesAsync();
    }
}