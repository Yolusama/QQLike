using System.Net;
using System.Net.Mail;
using QQLike.Entity.Configuration;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class EmailSender(EmailConfig config,IProjectLogger logger) : IEmailSender
{
    public void Send(string emailTo, string subject, string body)
    {
        var smtpServer = config.SmtpServer; // SMTP服务器地址
        var smtpPort = config.SmtpPort; // 通常587是TLS端口，465是SSL端口

        var mailMessage = new MailMessage
        {
            From = new MailAddress(config.Host,config.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true // 设置为false发送纯文本
        };

        mailMessage.To.Add(emailTo);

        using var smtpClient = new SmtpClient(smtpServer)
        {
            Port = smtpPort,
            Credentials = new NetworkCredential(config.Host, config.AuthorizationCode),
            EnableSsl = true, // 大多数现代SMTP服务器需要SSL
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        try
        {
            smtpClient.Send(mailMessage);
            logger.Log($"邮件已成功发送至{emailTo}","电子邮箱推送");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Log($"邮件发送失败至{emailTo}，异常信息：{e}","电子邮箱推送");
            throw;
        }
    }

    public async Task SendAsync(string emailTo, string subject, string body)
    {
        var smtpServer = config.SmtpServer; // SMTP服务器地址
        var smtpPort = config.SmtpPort; // 通常587是TLS端口，465是SSL端口

        var mailMessage = new MailMessage
        {
            From = new MailAddress(config.Host,config.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true // 设置为false发送纯文本
        };

        mailMessage.To.Add(emailTo);

        using var smtpClient = new SmtpClient(smtpServer)
        {
            Port = smtpPort,
            Credentials = new NetworkCredential(config.Host, config.AuthorizationCode),
            EnableSsl = true, // 大多数现代SMTP服务器需要SSL
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        try
        {
            await smtpClient.SendMailAsync(mailMessage);
            await logger.LogAsync($"邮件已成功发送至{emailTo}","电子邮箱推送");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"邮件发送失败至{emailTo}，异常信息：{e}","电子邮箱推送");
            throw;
        }
    }
}