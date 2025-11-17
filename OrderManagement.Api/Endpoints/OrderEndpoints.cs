using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.Dtos;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Endpoints;

public static class OrderEndpoints
{
    private static readonly ActivitySource _activitySource = new("OrderEndpoint");
    private static readonly Meter _meter = new("OrderEndpoint");
    private static readonly Counter<int> _orderRequestCounter = _meter.CreateCounter<int>(
        "order.requests",
        "orders",
        "Total number of order request"
    );
    private static readonly Counter<int> _orderItemCounter = _meter.CreateCounter<int>(
        "order.items"
    );

    public static void MapOrderEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/orders",
                async (
                    [FromBody] OrderPost orderPost,
                    OrderDbContext dbContext,
                    IHttpClientFactory httpClientFactory,
                    CancellationToken cancellationToken
                ) =>
                {
                    var sw = Stopwatch.StartNew();
                    using var activity = _activitySource.StartActivity("OrderProduct");
                    try
                    {
                        var httpClient = httpClientFactory.CreateClient();
                        var reserveDto = new ReserveDto() { Quantity = orderPost.Quantity };

                        activity?.AddEvent(new ActivityEvent("Reserving product..."));
                        var response = await httpClient.PutAsJsonAsync(
                            $"http://product-management:5001/product/{orderPost.ProductId}/reserve",
                            reserveDto
                        );

                        response.EnsureSuccessStatusCode();

                        var order = new Order()
                        {
                            UserName = orderPost.UserName,
                            TotalAmount = orderPost.Amount * orderPost.Quantity,
                            ProductId = orderPost.ProductId,
                            Amount = orderPost.Amount,
                            Quantity = orderPost.Quantity
                        };
                        await dbContext.Orders.AddAsync(order);
                        await dbContext.SaveChangesAsync();

                        activity?.AddEvent(new ActivityEvent("Successfully product reserved!!!"));

                        return Results.Ok("Order has been placed.");
                    }
                    catch (Exception ex)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, "Error placing order");
                        activity?.AddException(ex);
                        activity?.AddEvent(new ActivityEvent("Placing order failed"));
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("OrderProduct")
            .WithTags("Orders")
            .WithDescription("Order product");

        app.MapGet(
                "/orders",
                async (OrderDbContext dbContext) =>
                {
                    var orders = await dbContext.Orders.ToListAsync();
                    return Results.Ok(orders);
                }
            )
            .WithName("GetOrders")
            .WithTags("Orders")
            .WithDescription("Get all order");
    }
}

public record ReserveDto
{
    public int Quantity { get; set; }
}
