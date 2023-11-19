namespace SantaBot.Models;

public record User
{
    public int Id { get; set; }
    public int VkId { get; set; }
    public int Step { get; set; }
    public string Name { get; set; }
    public string Gift { get; set; }
    public int PointId { get; set; }
}