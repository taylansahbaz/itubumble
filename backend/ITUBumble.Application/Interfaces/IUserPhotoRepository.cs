using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITUBumble.Domain.Entities;

namespace ITUBumble.Application.Interfaces
{
    public interface IUserPhotoRepository
    {
        Task AddAsync(UserPhoto photo);
        Task<List<UserPhoto>> GetPhotosByUserIdAsync(Guid userId);
        Task<UserPhoto?> GetByIdAsync(Guid photoId);
        Task<UserPhoto?> GetProfilePhotoByUserIdAsync(Guid userId);
        Task RemoveAsync(UserPhoto photo);
        Task SaveChangesAsync();
        Task SetProfilePhotoAsync(Guid userId, Guid photoId);
    }
} 