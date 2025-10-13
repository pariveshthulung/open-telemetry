namespace OrderManagement.Api.Models;

public class Order
{
    public int Id { get; set; }
    public string UserName { get; set; } = default!;
    public int ProductId { get; set; }
    public int Amount { get; set; }
    public int Quantity { get; set; }
    public int TotalAmount { get; set; }
}
