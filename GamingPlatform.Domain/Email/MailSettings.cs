namespace GamingPlatform.Domain.Email
{
    public class MailSettings
    {
        public string? SmtpServer { get; set; }
        public int SmtpServerPort { get; set; }
        public string? SendersName { get; set; }
        public string? SmtpUserName { get; set; }
        public string? SmtpPassword { get; set; }
    }
}
