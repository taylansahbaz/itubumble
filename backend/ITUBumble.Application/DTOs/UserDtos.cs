using System;
using System.Collections.Generic;

namespace ITUBumble.Application.DTOs
{
    public class RegisterUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public int StudentNumber { get; set; }
    }

    public class LoginUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public int StudentNumber { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; }
        public UserResponseDto User { get; set; }
    }

    public class ResendVerificationEmailDto
    {
        public string Email { get; set; }
    }

    public class ProfileResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public int StudentNumber { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Bio { get; set; }
        public string? Interests { get; set; }
        public string? ProfileImageUrl { get; set; }
        public List<string> Photos { get; set; } = new List<string>();
    }

    public class UpdateProfileDto
    {
        public string? Bio { get; set; }
        public string? Interests { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
} 