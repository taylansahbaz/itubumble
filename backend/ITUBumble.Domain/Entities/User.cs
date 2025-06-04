using System;
using System.Collections.Generic;

namespace ITUBumble.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Department { get; private set; }
        public int StudentNumber { get; private set; }
        public bool IsEmailVerified { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        public string? EmailVerificationToken { get; private set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; private set; }
        public string? Bio { get; private set; }
        public string? Interests { get; private set; }
        public string? ProfileImageUrl { get; private set; }
        public ICollection<UserPhoto> Photos { get; private set; } = new List<UserPhoto>();

        private User() { } // For EF Core

        public User(string email, string passwordHash, string firstName, string lastName, 
                   string department, int studentNumber)
        {
            if (!email.EndsWith("@itu.edu.tr"))
                throw new ArgumentException("Email must be an ITU email address");

            Id = Guid.NewGuid();
            Email = email;
            PasswordHash = passwordHash;
            FirstName = firstName;
            LastName = lastName;
            Department = department;
            StudentNumber = studentNumber;
            IsEmailVerified = false;
            CreatedAt = DateTime.UtcNow;
        }

        public void VerifyEmail()
        {
            IsEmailVerified = true;
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public void SetEmailVerificationToken(string token, DateTime expiresAt)
        {
            EmailVerificationToken = token;
            EmailVerificationTokenExpiresAt = expiresAt;
        }

        public void ClearEmailVerificationToken()
        {
            EmailVerificationToken = null;
            EmailVerificationTokenExpiresAt = null;
        }

        public void UpdateProfile(string? bio, string? interests, string? profileImageUrl)
        {
            Bio = bio;
            Interests = interests;
            ProfileImageUrl = profileImageUrl;
        }
    }
} 