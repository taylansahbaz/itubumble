using System;
using System.Threading.Tasks;
using ITUBumble.Application.Interfaces;
using ITUBumble.Domain.Entities;
using ITUBumble.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITUBumble.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetByEmailVerificationTokenAsync(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        }
    }
} 