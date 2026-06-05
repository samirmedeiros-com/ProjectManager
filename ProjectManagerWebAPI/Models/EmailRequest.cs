namespace ProjectManagerWebAPI.Models;

public class EmailRequest
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string? HtmlBody { get; set; }
    public List<string>? CcEmails { get; set; }
    public List<string>? BccEmails { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = "";
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

public class EmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? MessageId { get; set; }
}
