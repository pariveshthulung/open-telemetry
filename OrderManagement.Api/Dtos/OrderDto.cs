namespace OrderManagement.Api.Dtos;

public class OrderDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = default!;
    public int TotalAmount { get; set; }
    public int ProductId { get; set; }
    public int Amount { get; set; }
    public int Quantity { get; set; }
}

public class OrderPost
{
    public string UserName { get; set; } = default!;
    public int ProductId { get; set; }
    public int Amount { get; set; }
    public int Quantity { get; set; }
}
