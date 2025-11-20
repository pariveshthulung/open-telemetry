using System.Diagnostics;

namespace ProductManagement.Api.Endpoints;

public static class TracesEndpoints
{
    private static ActivityContext? _lastCheckoutContext = null;

    public static void MapTracesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/traces/tags",
                (ActivitySource activitySource) =>
                {
                    using var activity = activitySource.StartActivity("TagsExample");

                    activity?.SetTag("user.id", 1234);
                    activity?.SetTag("user.name", "parivesh");

                    return Results.Ok(new { message = "Tags added to trace" });
                }
            )
            .WithName("OtelTags")
            .WithTags("OpenTelemetry");

        app.MapGet(
                "/traces/events",
                (ActivitySource activitySource) =>
                {
                    using var activity = activitySource.StartActivity("EventsExample");

                    activity?.AddEvent(new ActivityEvent("Started database lookup"));

                    Task.Delay(100);

                    activity?.AddEvent(new ActivityEvent("Finished DB lookup"));

                    activity?.AddEvent(new ActivityEvent("Returned response"));

                    return Results.Ok(new { message = "Events added to trace" });
                }
            )
            .WithName("OtelEvents")
            .WithTags("OpenTelemetry");

        app.MapGet(
                "/traces/exception",
                (ActivitySource activitySource) =>
                {
                    using var activity = activitySource.StartActivity("ExceptionExample");

                    try
                    {
                        throw new InvalidOperationException(
                            "Something went wrong in this endpoint!"
                        );
                    }
                    catch (Exception ex)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, "Exception thrown");
                        activity?.AddException(ex);

                        return Results.Problem("Error occurred: " + ex.Message);
                    }
                }
            )
            .WithName("OtelException")
            .WithTags("OpenTelemetry");

        app.MapGet(
                "/traces/nested",
                (ActivitySource activitySource) =>
                {
                    using var parent = activitySource.StartActivity("ParentSpan");

                    parent?.AddEvent(new ActivityEvent("Starting nested operations"));

                    using (var child = activitySource.StartActivity("ChildSpan"))
                    {
                        child?.SetTag("child.work", "processing");
                        Task.Delay(150);

                        using (var subChild = activitySource.StartActivity("SubChildSpan"))
                        {
                            subChild?.SetTag("subchild.work", "processing");
                        }
                    }

                    using (var db = activitySource.StartActivity("DBQuerySpan"))
                    {
                        Task.Delay(100);
                    }

                    return Results.Ok("Nested spans created");
                }
            )
            .WithName("OtelNested")
            .WithTags("OpenTelemetry");

        app.MapGet(
                "/traces/checkout",
                (ActivitySource activitySource) =>
                {
                    using var activity = activitySource.StartActivity(
                        "UserCheckout",
                        ActivityKind.Server
                    );

                    // Store trace context for later linking
                    _lastCheckoutContext = activity!.Context;

                    activity.AddEvent(new ActivityEvent("Checkout completed"));

                    return Results.Ok("Checkout completed. Context saved.");
                }
            )
            .WithName("OtelCheckout")
            .WithTags("OpenTelemetry");

        app.MapGet(
                "/traces/retry-payment",
                (ActivitySource activitySource) =>
                {
                    if (_lastCheckoutContext == null)
                        return Results.BadRequest(
                            "No checkout trace found. Call /otel/checkout first."
                        );

                    var link = new ActivityLink(_lastCheckoutContext.Value);

                    using var activity = activitySource.StartActivity(
                        "RetryPayment",
                        ActivityKind.Server,
                        default(ActivityContext), // new trace, not parent/child
                        links: new[] { link }
                    );

                    activity?.AddEvent(
                        new ActivityEvent("Retry payment linked with checkout trace")
                    );

                    return Results.Ok("Payment retried. Linked to checkout trace.");
                }
            )
            .WithName("OtelRetryPayment")
            .WithTags("OpenTelemetry");
    }
}
