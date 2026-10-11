namespace FlemanApi.Service;

// Thin SMTP wrapper (MailKit) — the thing actually queued/dispatched by
// the background email worker, mirroring JavaMailSender in the Java app.
public interface IEmailSender
{
    Task SendPlainAsync(string to, string subject, string body);
    Task SendWithAttachmentAsync(string to, string subject, string body, string attachmentName, byte[] attachment);
}
