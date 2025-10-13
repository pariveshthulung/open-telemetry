namespace OrderManagement.Api.Models;

public class OrderDetails
{
    public int Id { get; set; }
    public string ProductName { get; set; } = default!;
    public int ProductId { get; set; }
    public int Amount { get; set; }
    public int Quantity { get; set; }
}
