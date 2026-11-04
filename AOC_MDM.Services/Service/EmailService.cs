using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using System.Net.Mail;


namespace AOC_MDM.Services.Service
{

    public class EmailService : IEmailService
    {
      //  private readonly IConfiguration _configuration;

        public EmailService()//IConfiguration configuration)
        {
          //  _configuration = configuration;
        }

        public  async Task SendEmailAsync(string mailTo, string[] ccArray, string subject, string text, string environmentType, bool IsBodyHtml = false)
        {
            try
            {
                using (SmtpClient client = new SmtpClient("mailman.supreme.com"))
                using (MailMessage message = new MailMessage())
                {
                    message.From = new MailAddress("DoNotReply@sdsRequest.com", "NoReply");
                    List<string> listMailTo = mailTo.Split(new char[] { ',', ';' }).ToList();
                    foreach (string mailAddress in listMailTo)
                    {
                        message.To.Add(mailAddress);
                    }
                    if (ccArray != null)
                    {
                        foreach (string mailAddress in ccArray)
                        {
                            message.CC.Add(mailAddress);
                        }
                    }


                    string appUrl = string.Empty;

                    // Determine the application environment
                    //if (Connections.isProduction())
                    //{
                    //    appUrl = $"https://localhost:7033/VendorRefAttri/Approve?id={vendorId}";
                    //}
                    //else if (Connections.isUAT())
                    //{
                    //    appUrl = $"https://localhost:7033/VendorRefAttri/Approve?id={vendorId}";
                    //}
                    //else if (Connections.isDev())
                    //{
                    //    appUrl = $"https://localhost:7033/VendorRefAttri/Approve?id={vendorId}";
                    //}


                    message.Subject = $@"[{environmentType}] {subject}";
                    message.IsBodyHtml = IsBodyHtml;
                    message.Body = text;

                    await client.SendMailAsync(message);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public  string GetEmailBody(Exception ex, string clientName, string supportEmail, string supportPhone, string companyName)
        {
            string serviceName = "Unknown";
            //if (ex.StackTrace.Contains("shopify", StringComparison.OrdinalIgnoreCase))
            //{
            //    serviceName = "Shopify";
            //}
            //else if (ex.StackTrace.Contains("paypal", StringComparison.OrdinalIgnoreCase))
            //{
            //    serviceName = "Paypal";
            //}
            //else if (ex.StackTrace.Contains("adyen", StringComparison.OrdinalIgnoreCase))
            //{
            //    serviceName = "Adyen";
            //}

            string emailBody = EMAIL_BODYHTML
                .Replace("[Client Name]", clientName)
                .Replace("{ServiceName}", serviceName)
                .Replace("{ExceptionType}", ex.GetType().Name)
                .Replace("{ExceptionMessage}", ex.Message)
                .Replace("{StackTrace}", ex.StackTrace)
                .Replace("{OccurredAt}", DateTime.UtcNow.ToString())
                .Replace("{SupportEmail}", supportEmail)
                .Replace("{SupportPhone}", supportPhone)
                .Replace("{CompanyName}", companyName);

            return emailBody;
        }


        public const string EMAIL_BODYHTML =
   $@"
 <html>
        <body>    
<div style='font-family: Arial; font-size: 1rem;'>
                <p>An edit request has been submitted for <b>[VendorId]</b> by <b>[UserName]</b>.</p>
                <p>Details:</p>
                <ul>
                    <li><b>Requested Date:</b> [RequestedDate]</li>
                    <li><b>Edited Fields:</b> [EditedFields]</li>
                    <li><b>Comments:</b> [Comments]</li>
                </ul>
                <p>Access this request using the <a href='[appUrl]'>SDS Request Management tool</a>.</p>
                <p>&nbsp;</p>
            </div>
    ";

    }
}
