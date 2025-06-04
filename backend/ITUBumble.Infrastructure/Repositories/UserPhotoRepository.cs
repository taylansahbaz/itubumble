using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITUBumble.Application.Interfaces;
using ITUBumble.Domain.Entities;
using ITUBumble.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITUBumble.Infrastructure.Repositories
{
    public class UserPhotoRepository : IUserPhotoRepository
    {
        private readonly ApplicationDbContext _context;
        public UserPhotoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserPhoto photo)
        {
            await _context.Set<UserPhoto>().AddAsync(photo);
        }

        public async Task<List<UserPhoto>> GetPhotosByUserIdAsync(Guid userId)
        {
            return await _context.Set<UserPhoto>()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.IsProfilePhoto)
                .ThenByDescending(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<UserPhoto?> GetByIdAsync(Guid photoId)
        {
            return await _context.Set<UserPhoto>().FirstOrDefaultAsync(p => p.Id == photoId);
        }

        public async Task<UserPhoto?> GetProfilePhotoByUserIdAsync(Guid userId)
        {
            return await _context.Set<UserPhoto>().FirstOrDefaultAsync(p => p.UserId == userId && p.IsProfilePhoto);
        }

        public async Task RemoveAsync(UserPhoto photo)
        {
            _context.Set<UserPhoto>().Remove(photo);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task SetProfilePhotoAsync(Guid userId, Guid photoId)
        {
            var photos = await _context.Set<UserPhoto>().Where(p => p.UserId == userId).ToListAsync();
            foreach (var photo in photos)
            {
                photo.IsProfilePhoto = (photo.Id == photoId);
            }
            await _context.SaveChangesAsync();
        }
    }
} 