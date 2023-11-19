namespace SantaBot.Models;

public record AppSettings
{
    public string Token { get; init; } = "";
    public long AdminId { get; init; }
    public ulong GroupId { get; init; }
    public bool RegistrationOpened { get; set; }
}