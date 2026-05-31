namespace FITSync.Contracts.AdditionalServices;

public class AdditionalServiceResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
