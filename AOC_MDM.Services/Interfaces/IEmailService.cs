using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string mailTo, string[] ccArray, string subject, string text, string environmentType, bool IsBodyHtml = false);

        string GetEmailBody(Exception ex, string clientName, string supportEmail, string supportPhone, string companyName);


    }
}
