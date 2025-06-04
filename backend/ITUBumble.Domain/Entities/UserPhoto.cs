using System;

namespace ITUBumble.Domain.Entities
{
    public class UserPhoto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Url { get; set; } = null!;
        public bool IsProfilePhoto { get; set; }
        public DateTime UploadedAt { get; set; }

        public UserPhoto() { }
        public UserPhoto(Guid userId, string url, bool isProfilePhoto = false)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Url = url;
            IsProfilePhoto = isProfilePhoto;
            UploadedAt = DateTime.UtcNow;
        }
    }
} 