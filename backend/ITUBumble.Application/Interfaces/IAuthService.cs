using System;
using System.Threading.Tasks;
using ITUBumble.Application.DTOs;

namespace ITUBumble.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginUserDto loginDto);
        Task<UserResponseDto> GetUserByIdAsync(Guid userId);
        Task<bool> VerifyEmailAsync(string token);
        Task<bool> ResendVerificationEmailAsync(string email);
        Task<ProfileResponseDto> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    }
} 