namespace PulseHR.Api.DTOs.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public bool Read { get; set; }
}
