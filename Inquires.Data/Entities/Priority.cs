namespace Inquires.Data;

public class Priority
{
    public int PriorityId { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
}
