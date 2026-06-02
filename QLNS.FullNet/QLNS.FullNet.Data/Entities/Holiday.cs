namespace QLNS.FullNet.Data.Entities;

public class Holiday
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}
