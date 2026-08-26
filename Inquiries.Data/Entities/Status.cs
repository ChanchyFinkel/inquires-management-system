namespace Inquiries.Data;

public class Status
{
    public int StatusId { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
}
