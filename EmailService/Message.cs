using MimeKit;

namespace EmailService;

public class Message
{
    public string From { get; set; }
    public string SenderName { get; set; }
    public string To { get; set; }
    public string ReceiverName { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }

    public Message(string from, string senderName, string to, string receiverName, string subject, string content)
    {
        From = from;
        SenderName = senderName;
        To = to;
        ReceiverName = receiverName;
        Subject = subject;
        Content = content;
    }
}