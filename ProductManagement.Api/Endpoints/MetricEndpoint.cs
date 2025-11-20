using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ProductManagement.Api.Endpoints;

public static class MetricEndpoint
{
    private static readonly Meter _meter = new("ProductEndpoint");
    private static readonly Counter<int> _ordersPlacedCounter = _meter.CreateCounter<int>(
        "orders.placed",
        "orders",
        "Number of orders successfully placed"
    );

    // UpDownCounter → tracks inventory changes
    private static readonly UpDownCounter<int> _inventoryLevelCounter =
        _meter.CreateUpDownCounter<int>(
            "inventory.level",
            "items",
            "Tracks inventory changes (+add, -remove)"
        );

    // Histogram → measures processing time
    private static readonly Histogram<double> _orderProcessingHistogram =
        _meter.CreateHistogram<double>(
            "order.processing.duration",
            "ms",
            "Distribution of order processing durations"
        );

    public static void MapMetricEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/orders",
                () =>
                {
                    // Increment order counter
                    _ordersPlacedCounter.Add(1);

                    return Results.Ok(new { Message = "Order placed", OrderId = Guid.NewGuid() });
                }
            )
            .WithTags("OpenTelemetry");

        app.MapPost(
                "/inventory/update",
                (int change) =>
                {
                    // Add or remove inventory
                    _inventoryLevelCounter.Add(change);

                    return Results.Ok(new { Message = "Inventory updated", Change = change });
                }
            )
            .WithTags("OpenTelemetry");
        app.MapPost(
                "/orders/process",
                async () =>
                {
                    var sw = Stopwatch.StartNew();

                    // Simulate processing work
                    await Task.Delay(Random.Shared.Next(50, 300));

                    sw.Stop();

                    // Record processing duration in milliseconds
                    _orderProcessingHistogram.Record(sw.Elapsed.TotalMilliseconds);

                    return Results.Ok(
                        new
                        {
                            Message = "Order processed",
                            DurationMs = sw.Elapsed.TotalMilliseconds
                        }
                    );
                }
            )
            .WithTags("OpenTelemetry");
    }
}
