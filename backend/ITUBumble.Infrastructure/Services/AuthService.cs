using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ITUBumble.Application.DTOs;
using ITUBumble.Application.Interfaces;
using ITUBumble.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;

namespace ITUBumble.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly IUserPhotoRepository _userPhotoRepository;

        public AuthService(
            IConfiguration configuration, 
            IUserRepository userRepository,
            ILogger<AuthService> logger,
            IEmailService emailService,
            IUserPhotoRepository userPhotoRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _logger = logger;
            _emailService = emailService;
            _userPhotoRepository = userPhotoRepository;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
            if (existingUser != null)
                throw new ArgumentException("User with this email already exists");

            // Create new user
            var passwordHash = BC.HashPassword(registerDto.Password);
            var user = new User(
                registerDto.Email,
                passwordHash,
                registerDto.FirstName,
                registerDto.LastName,
                registerDto.Department,
                registerDto.StudentNumber
            );

            // Email verification token oluştur
            var emailToken = Guid.NewGuid().ToString("N");
            var tokenExpires = DateTime.UtcNow.AddHours(24); // 24 saat geçerli
            user.SetEmailVerificationToken(emailToken, tokenExpires);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Doğrulama linki oluştur
            var verificationUrl = $"http://localhost:5288/api/auth/verify-email?token={emailToken}";
            var emailBody = $"Merhaba {user.FirstName},<br/><br/>Hesabını doğrulamak için aşağıdaki linke tıkla:<br/><a href='{verificationUrl}'>{verificationUrl}</a><br/><br/>Bu link 24 saat geçerlidir.";
            await _emailService.SendEmailAsync(user.Email, "İTÜBumble Hesap Doğrulama", emailBody);

            // Generate JWT token
            var token = GenerateJwtToken(user);
            _logger.LogInformation("Generated token for user {UserId}: {Token}", user.Id, token);

            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserResponseDto(user)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginUserDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
                throw new ArgumentException("Invalid email or password");

            if (!BC.Verify(loginDto.Password, user.PasswordHash))
                throw new ArgumentException("Invalid email or password");

            user.UpdateLastLogin();
            await _userRepository.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            _logger.LogInformation("Generated token for user {UserId}: {Token}", user.Id, token);

            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserResponseDto(user)
            };
        }

        public async Task<UserResponseDto> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            return MapToUserResponseDto(user);
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var user = await _userRepository.GetByEmailVerificationTokenAsync(token);
            if (user == null)
                return false;
            if (user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
                return false;
            user.VerifyEmail();
            user.ClearEmailVerificationToken();
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResendVerificationEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return false;
            if (user.IsEmailVerified)
                return false;

            // Yeni token üret
            var emailToken = Guid.NewGuid().ToString("N");
            var tokenExpires = DateTime.UtcNow.AddHours(24);
            user.SetEmailVerificationToken(emailToken, tokenExpires);
            await _userRepository.SaveChangesAsync();

            // Doğrulama linki oluştur
            var verificationUrl = $"http://localhost:5288/api/auth/verify-email?token={emailToken}";
            var emailBody = $"Merhaba {user.FirstName},<br/><br/>Hesabını doğrulamak için aşağıdaki linke tıkla:<br/><a href='{verificationUrl}'>{verificationUrl}</a><br/><br/>Bu link 24 saat geçerlidir.";
            await _emailService.SendEmailAsync(user.Email, "İTÜBumble Hesap Doğrulama (Tekrar)", emailBody);
            return true;
        }

        public async Task<ProfileResponseDto> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            var photos = await _userPhotoRepository.GetPhotosByUserIdAsync(userId);
            var profilePhoto = photos.FirstOrDefault(p => p.IsProfilePhoto);

            return new ProfileResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Department,
                StudentNumber = user.StudentNumber,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                Bio = user.Bio,
                Interests = user.Interests,
                ProfileImageUrl = profilePhoto?.Url,
                Photos = photos.Select(p => p.Url).ToList()
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;
            user.UpdateProfile(dto.Bio, dto.Interests, dto.ProfileImageUrl);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found")));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                _logger.LogInformation("Creating token for user {UserId} with claims: {@Claims}", user.Id, claims);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(7),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogInformation("Token generated successfully for user {UserId}", user.Id);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token for user {UserId}", user.Id);
                throw;
            }
        }

        private static UserResponseDto MapToUserResponseDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Department,
                StudentNumber = user.StudentNumber,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt
            };
        }
    }
} 