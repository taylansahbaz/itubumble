using System.Threading.Tasks;

namespace ITUBumble.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
} 