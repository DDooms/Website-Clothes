using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;
using Task = System.Threading.Tasks.Task;

namespace EmailService;

public class EmailSender : IEmailSender
{
    private readonly EmailConfiguration EmailConfiguration;

    public EmailSender(EmailConfiguration emailConfiguration)
    {
        EmailConfiguration = emailConfiguration;
    }

    public async Task SendEmailAsync(Message message)
    {
        var apiInstance = new TransactionalEmailsApi();

        var Email = new SendSmtpEmailSender(message.SenderName, message.From);

        var smtpEmailTo = new SendSmtpEmailTo(message.To, message.ReceiverName);
        var To = new List<SendSmtpEmailTo> { smtpEmailTo };

        string HtmlContent = message.Content;
        string TextContent = null;

        try
        {
            var sendSmtpEmail = new SendSmtpEmail(Email, To, null, null, HtmlContent, TextContent, message.Subject);
            CreateSmtpEmail result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            Console.WriteLine(result.ToJson());
        }
        catch
        {
            throw;
        }
    }
}