namespace QQLike.Functional.Instructure;

public interface IEmailSender
{
    public void Send(string emailTo, string subject,string body);
    public Task SendAsync(string emailTo, string subject,string body);
}