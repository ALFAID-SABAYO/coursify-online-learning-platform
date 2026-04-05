using System.Net;
using System.Net.Mail;

public class EmailService
{
    public void SendEmail(string toEmail, string subject, string body)
    {
        var fromEmail = "******************";
        var fromPassword = "***************"; // <--- use app password here

        var smtp = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new NetworkCredential(fromEmail, fromPassword),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage(fromEmail, toEmail, subject, body);
        mailMessage.IsBodyHtml = true;

        smtp.Send(mailMessage);
    }
}
