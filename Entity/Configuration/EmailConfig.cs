namespace QQLike.Entity.Configuration;

public class EmailConfig
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string Host { get; set; }
    public string AuthorizationCode {get; set; }
    public string DisplayName { get; set; }
}