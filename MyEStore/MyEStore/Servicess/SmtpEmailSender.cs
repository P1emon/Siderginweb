using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public void SendEmail(string to, string subject, string body)
    {
        var client = new SmtpClient(_config["Smtp:Host"])
        {
            Port = int.Parse(_config["Smtp:Port"]),
            Credentials = new NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
            EnableSsl = true
        };

        var mail = new MailMessage(_config["Smtp:From"], to, subject, body)
        {
            IsBodyHtml = true
        };

        client.Send(mail);
    }
}
