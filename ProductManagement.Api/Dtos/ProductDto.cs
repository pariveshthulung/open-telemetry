namespace ProductManagement.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public record ReserveDto
{
    public int Quantity { get; set; }
}
