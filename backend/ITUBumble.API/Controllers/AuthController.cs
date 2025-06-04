using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ITUBumble.Application.DTOs;
using ITUBumble.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using ITUBumble.Domain.Entities;
using System.Collections.Generic;

namespace ITUBumble.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserPhotoRepository _userPhotoRepository;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IUserPhotoRepository userPhotoRepository)
        {
            _authService = authService;
            _logger = logger;
            _userPhotoRepository = userPhotoRepository;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterUserDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while registering the user." });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginUserDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while logging in." });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDto>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("User ID claim not found in token");
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                if (!Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Invalid user ID format in token: {UserId}", userIdClaim.Value);
                    return Unauthorized(new { message = "Invalid token: Invalid user ID format" });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId}", userId);
                    return Unauthorized(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentUser");
                return StatusCode(500, new { message = "An error occurred while fetching user data." });
            }
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _authService.VerifyEmailAsync(token);
            if (result)
                return Ok(new { message = "E-posta başarıyla doğrulandı." });
            return BadRequest(new { message = "Geçersiz veya süresi dolmuş doğrulama linki." });
        }

        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailDto dto)
        {
            var result = await _authService.ResendVerificationEmailAsync(dto.Email);
            if (result)
                return Ok(new { message = "Doğrulama e-postası tekrar gönderildi." });
            return BadRequest(new { message = "Kullanıcı bulunamadı veya zaten doğrulanmış." });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<ProfileResponseDto>> GetProfile()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            var profile = await _authService.GetProfileAsync(userId);
            return Ok(profile);
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            var result = await _authService.UpdateProfileAsync(userId, dto);
            if (result)
                return Ok(new { message = "Profil güncellendi." });
            return BadRequest(new { message = "Profil güncellenemedi." });
        }

        [Authorize]
        [HttpPost("profile-images")]
        public async Task<IActionResult> UploadProfileImages([FromForm] List<IFormFile> files)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (files == null || files.Count == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uploadedImages = new List<string>();
            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;
                if (!allowedTypes.Contains(file.ContentType)) continue;

                var fileName = $"{userId}_{Guid.NewGuid().ToString("N")}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var imageUrl = $"{baseUrl}/profile-images/{fileName}";
                var photo = new UserPhoto(userId, imageUrl);
                await _userPhotoRepository.AddAsync(photo);
                uploadedImages.Add(imageUrl);
            }
            await _userPhotoRepository.SaveChangesAsync();

            return Ok(new { images = uploadedImages, message = "Fotoğraflar yüklendi." });
        }

        [Authorize]
        [HttpGet("photos")]
        public async Task<IActionResult> GetPhotos()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            var photos = await _userPhotoRepository.GetPhotosByUserIdAsync(userId);
            return Ok(photos);
        }

        [Authorize]
        [HttpPost("photos/{photoId}/set-profile")]
        public async Task<IActionResult> SetProfilePhoto(Guid photoId)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            await _userPhotoRepository.SetProfilePhotoAsync(userId, photoId);
            return Ok(new { message = "Profil fotoğrafı olarak ayarlandı." });
        }

        [Authorize]
        [HttpDelete("photos")]
        public async Task<IActionResult> DeletePhotos([FromBody] List<Guid> photoIds)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            if (photoIds == null || photoIds.Count == 0)
                return BadRequest(new { message = "Silinecek fotoğraf seçilmedi." });

            int deletedCount = 0;
            foreach (var photoId in photoIds)
            {
                var photo = await _userPhotoRepository.GetByIdAsync(photoId);
                if (photo == null || photo.UserId != userId)
                    continue;
                // Dosyayı sunucudan sil
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-images", Path.GetFileName(new Uri(photo.Url).LocalPath));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
                await _userPhotoRepository.RemoveAsync(photo);
                deletedCount++;
            }
            await _userPhotoRepository.SaveChangesAsync();
            return Ok(new { message = $"{deletedCount} fotoğraf silindi." });
        }
    }
} 