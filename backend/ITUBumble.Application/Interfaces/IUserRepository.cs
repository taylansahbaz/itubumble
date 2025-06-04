using System;
using System.Threading.Tasks;
using ITUBumble.Domain.Entities;

namespace ITUBumble.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByEmailVerificationTokenAsync(string token);
        Task AddAsync(User user);
        Task SaveChangesAsync();
    }
} 