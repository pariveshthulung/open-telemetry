using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using ProductManagement.Api.Data;
using ProductManagement.Api.Dtos;
using ProductManagement.Api.Models;

namespace ProductManagement.Api.Endpoints;

public static class ProductEndpoints
{
    private static readonly ActivitySource _activitySource = new("ProductEndpoint");
    private static readonly Meter _meter = new("ProductEndpoint");
    private static readonly Counter<int> _productRequestCounter = _meter.CreateCounter<int>(
        "product.requests",
        "products",
        "Total number of product request"
    );
    private static readonly Counter<int> _productItemCounter = _meter.CreateCounter<int>(
        "product.items"
    );

    [Obsolete]
    public static void MapProductEndpoints(
        this IEndpointRouteBuilder app,
        ILoggerFactory loggerFactory
    )
    {
        var logger = loggerFactory.CreateLogger("ProductEndpoint");
        app.MapGet(
                "/products",
                async (ProductDbContext context, CancellationToken cancellationToken) =>
                {
                    using var activity = _activitySource.StartActivity("GetProduct");
                    _productRequestCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("endpoint", "/api/products")
                    );
                    try
                    {
                        activity?.AddEvent(new ActivityEvent("Fetching product...."));
                        var products = await context.Products.ToListAsync(cancellationToken);
                        return Results.Ok(products);
                    }
                    catch (Exception ex)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, "Error fetching Products");
                        activity?.AddException(ex);
                        activity?.AddEvent(new ActivityEvent("Fetching Products failed"));
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("GetProducts")
            .WithTags("Products")
            .WithDescription("Get all products");

        app.MapGet(
                "/products/{id:int}",
                async (
                    [FromRoute] int id,
                    ProductDbContext context,
                    CancellationToken cancellationToken
                ) =>
                {
                    using var activity = _activitySource.StartActivity("GetProductById");
                    try
                    {
                        activity?.SetTag("product.id", id);
                        var product = await context.Products.FirstOrDefaultAsync(
                            x => x.Id == id,
                            cancellationToken
                        );
                        if (product is null)
                            // throw new Exception("Product not found");
                            return Results.NotFound();
                        _productItemCounter.Add(
                            1,
                            new("ProductId", product.Id),
                            new("ProductName", product.Name)
                        );
                        return Results.Ok(product);
                    }
                    catch (Exception ex)
                    {
                        activity?.SetStatus(
                            ActivityStatusCode.Error,
                            "Error fetching Product by id"
                        );
                        activity?.AddException(ex);
                        activity?.AddEvent(new ActivityEvent("Fetching Product failed"));
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("GetProductById")
            .WithTags("Products")
            .WithDescription("Get product by id");

        app.MapPost(
                "/products",
                async (
                    [FromBody] ProductDto productDto,
                    ProductDbContext context,
                    CancellationToken cancellationToken
                ) =>
                {
                    using var activity = _activitySource.StartActivity("AddProduct");
                    try
                    {
                        var product = new Product
                        {
                            Id = productDto.Id,
                            Name = productDto.Name,
                            Price = productDto.Price,
                            Quantity = productDto.Quantity
                        };

                        await context.Products.AddAsync(product, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);

                        return Results.CreatedAtRoute(
                            "GetProductById",
                            new { id = product.Id },
                            product
                        );
                    }
                    catch (Exception ex)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, "Error adding Product");
                        activity?.AddException(ex);
                        activity?.AddEvent(new ActivityEvent("Add Product failed"));
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("AddProduct")
            .WithTags("Products")
            .WithDescription("Add product");

        app.MapPut(
                "/products/{id:int}",
                async (
                    [FromRoute] int id,
                    [FromBody] ProductDto productDto,
                    ProductDbContext context,
                    CancellationToken cancellationToken
                ) =>
                {
                    try
                    {
                        var product = await context.Products.FirstOrDefaultAsync(
                            x => x.Id == id,
                            cancellationToken
                        );
                        if (product is null)
                            return Results.NotFound();

                        product.Name = productDto.Name;
                        product.Price = productDto.Price;
                        product.Quantity = productDto.Quantity;

                        context.Products.Update(product);
                        await context.SaveChangesAsync(cancellationToken);
                        return Results.NoContent();
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("UpdateProduct")
            .WithTags("Products")
            .WithDescription("Update product");

        app.MapPut(
                "/product/{id:int}/reserve",
                async (
                    [FromRoute] int id,
                    [FromBody] ReserveDto reserveDto,
                    ProductDbContext context
                ) =>
                {
                    using var activity = _activitySource.StartActivity("ReserveProduct");
                    try
                    {
                        var product = await context.Products.FirstOrDefaultAsync(x => x.Id == id);
                        if (product is null)
                        {
                            activity?.SetStatus(ActivityStatusCode.Error, "Product does not exist");
                            activity?.SetTag("error.type", "BadRequest");
                            activity?.SetTag("http.status_code", 400);
                            return Results.BadRequest("Product does not exist");
                        }
                        if (product.Quantity < 1 || product.Quantity - reserveDto.Quantity < 0)
                        {
                            activity?.SetStatus(ActivityStatusCode.Error, "Out of stock");
                            return Results.BadRequest("Out of stock");
                        }
                        product.Quantity -= reserveDto.Quantity;

                        context.Products.Update(product);
                        await context.SaveChangesAsync();
                        return Results.Ok("Product reserved successfully");
                    }
                    catch (Exception ex)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, "Error reserving Product");
                        activity?.SetTag("product.id", id);
                        activity?.SetTag("quantity", reserveDto.Quantity);
                        activity?.RecordException(ex);
                        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        activity?.AddEvent(new ActivityEvent("reserve product failed"));
                        throw;
                    }
                }
            )
            .WithName("ReserveProduct")
            .WithTags("Products")
            .WithDescription("Reserve product");
    }
}
