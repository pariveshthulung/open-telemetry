using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Data;
using ProductManagement.Api.Dtos;
using ProductManagement.Api.Models;

namespace ProductManagement.Api.Endpoints;

public static class ProductEndpoints
{
    private static readonly Meter _meter = new("ProductEndpoint");

    private static readonly Counter<int> _productReserveCounter = _meter.CreateCounter<int>(
        "product.reserve",
        "products",
        "Total number of reserved products"
    );

    private static readonly Counter<int> _requestCounter = _meter.CreateCounter<int>(
        "product.requests",
        description: "Total number of product requests"
    );

    private static readonly Counter<int> _itemCounter = _meter.CreateCounter<int>(
        "product.items",
        description: "Number of products processed"
    );

    public static void MapProductEndpoints(
        this IEndpointRouteBuilder app,
        ILoggerFactory loggerFactory
    )
    {
        var logger = loggerFactory.CreateLogger("ProductEndpoint");

        // GET /products
        app.MapGet(
                "/products",
                async (
                    ProductDbContext context,
                    CancellationToken ct,
                    ActivitySource activitySource
                ) =>
                {
                    using var activity = activitySource.StartActivity("GetProducts");
                    _requestCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("endpoint", "/products")
                    );

                    try
                    {
                        activity?.AddEvent(new ActivityEvent("Fetching all products"));
                        var products = await context.Products.ToListAsync(ct);
                        logger.LogInformation("Fetched {Count} products", products.Count);
                        return Results.Ok(products);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error fetching products");
                        activity?.SetStatus(ActivityStatusCode.Error, "Error fetching products");
                        activity?.AddException(ex);
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("GetProducts")
            .WithTags("Products");

        // GET /products/{id}
        app.MapGet(
                "/products/{id:int}",
                async (
                    int id,
                    ProductDbContext context,
                    CancellationToken ct,
                    ActivitySource activitySource
                ) =>
                {
                    using var activity = activitySource.StartActivity("GetProductById");
                    activity?.SetTag("product.id", id);
                    _requestCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("endpoint", "/products/{id}")
                    );

                    try
                    {
                        var product = await context.Products.FirstOrDefaultAsync(
                            p => p.Id == id,
                            ct
                        );
                        if (product is null)
                        {
                            logger.LogWarning("Product with id {Id} not found", id);
                            return Results.NotFound();
                        }

                        _itemCounter.Add(
                            1,
                            new KeyValuePair<string, object?>("product.id", product.Id)
                        );
                        logger.LogInformation(
                            "Fetched product {Id} - {Name}",
                            product.Id,
                            product.Name
                        );
                        return Results.Ok(product);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error fetching product by id {Id}", id);
                        activity?.SetStatus(
                            ActivityStatusCode.Error,
                            "Error fetching product by id"
                        );
                        activity?.AddException(ex);
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("GetProductById")
            .WithTags("Products");

        // POST /products
        app.MapPost(
                "/products",
                async (
                    ProductDto productDto,
                    ProductDbContext context,
                    CancellationToken ct,
                    ActivitySource activitySource
                ) =>
                {
                    using var activity = activitySource.StartActivity("AddProduct");
                    _requestCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("endpoint", "/products")
                    );

                    try
                    {
                        var product = new Product
                        {
                            Name = productDto.Name,
                            Price = productDto.Price,
                            Quantity = productDto.Quantity
                        };

                        await context.Products.AddAsync(product, ct);
                        await context.SaveChangesAsync(ct);

                        _itemCounter.Add(
                            1,
                            new KeyValuePair<string, object?>("product.id", product.Id)
                        );
                        logger.LogInformation(
                            "Added new product {Id} - {Name}",
                            product.Id,
                            product.Name
                        );

                        return Results.CreatedAtRoute(
                            "GetProductById",
                            new { id = product.Id },
                            product
                        );
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error adding product");
                        activity?.SetStatus(ActivityStatusCode.Error, "Error adding product");
                        activity?.AddException(ex);
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("AddProduct")
            .WithTags("Products");

        // PUT /products/{id}
        app.MapPut(
                "/products/{id:int}",
                async (
                    int id,
                    ProductDto productDto,
                    ProductDbContext context,
                    CancellationToken ct,
                    ActivitySource activitySource
                ) =>
                {
                    using var activity = activitySource.StartActivity("UpdateProduct");
                    activity?.SetTag("product.id", id);
                    _requestCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("endpoint", "/products/{id}")
                    );

                    try
                    {
                        var product = await context.Products.FirstOrDefaultAsync(
                            p => p.Id == id,
                            ct
                        );
                        if (product is null)
                        {
                            logger.LogWarning("Product with id {Id} not found for update", id);
                            return Results.NotFound();
                        }

                        product.Name = productDto.Name;
                        product.Price = productDto.Price;
                        product.Quantity = productDto.Quantity;

                        context.Products.Update(product);
                        await context.SaveChangesAsync(ct);

                        _itemCounter.Add(
                            1,
                            new KeyValuePair<string, object?>("product.id", product.Id)
                        );
                        logger.LogInformation(
                            "Updated product {Id} - {Name}",
                            product.Id,
                            product.Name
                        );

                        return Results.NoContent();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error updating product {Id}", id);
                        activity?.SetStatus(ActivityStatusCode.Error, "Error updating product");
                        activity?.AddException(ex);
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("UpdateProduct")
            .WithTags("Products");

        // DELETE /products/{id}
        app.MapDelete(
                "/products/{id:int}",
                async (
                    int id,
                    ProductDbContext context,
                    CancellationToken ct,
                    ActivitySource activitySource
                ) =>
                {
                    using var activity = activitySource.StartActivity("DeleteProduct");
                    activity?.SetTag("product.id", id);
                    _requestCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("endpoint", "/products/{id}")
                    );

                    try
                    {
                        var product = await context.Products.FirstOrDefaultAsync(
                            p => p.Id == id,
                            ct
                        );
                        if (product is null)
                        {
                            logger.LogWarning("Product with id {Id} not found for deletion", id);
                            return Results.NotFound();
                        }

                        context.Products.Remove(product);
                        await context.SaveChangesAsync(ct);

                        _itemCounter.Add(
                            1,
                            new KeyValuePair<string, object?>("product.id", product.Id)
                        );
                        logger.LogInformation(
                            "Deleted product {Id} - {Name}",
                            product.Id,
                            product.Name
                        );

                        return Results.NoContent();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error deleting product {Id}", id);
                        activity?.SetStatus(ActivityStatusCode.Error, "Error deleting product");
                        activity?.AddException(ex);
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("DeleteProduct")
            .WithTags("Products");

        // Reserve endpoint
        app.MapPut(
                "/product/{productId}/reserve",
                async (
                    int productId,
                    ReserveDto reserveDto,
                    ProductDbContext dbContext,
                    ActivitySource activitySource
                ) =>
                {
                    using var activity = activitySource.StartActivity("ReserveProduct");
                    try
                    {
                        var product = await dbContext.Products.FindAsync(productId);
                        if (product == null)
                        {
                            activity?.SetStatus(ActivityStatusCode.Error, "Product not found");
                            return Results.NotFound($"Product {productId} not found");
                        }

                        if (product.Quantity < reserveDto.Quantity)
                        {
                            activity?.SetStatus(ActivityStatusCode.Error, "Insufficient stock");
                            logger.LogError("Error reserving products");
                            return Results.BadRequest("Insufficient stock");
                        }

                        product.Quantity -= reserveDto.Quantity;
                        await dbContext.SaveChangesAsync();

                        _productReserveCounter.Add(
                            reserveDto.Quantity,
                            new KeyValuePair<string, object?>("productId", productId.ToString()),
                            new KeyValuePair<string, object?>(
                                "productName",
                                product.Name.ToString()
                            ),
                            new KeyValuePair<string, object?>("DateTime", DateTime.Now.ToString())
                        );

                        activity?.AddEvent(
                            new ActivityEvent(
                                $"Reserved {reserveDto.Quantity} units of product {productId}"
                            )
                        );

                        return Results.Ok(
                            $"Reserved {reserveDto.Quantity} units of product {productId}"
                        );
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error reserving products");
                        activity?.SetStatus(ActivityStatusCode.Error, "Error reserving product");
                        activity?.AddException(ex);
                        return Results.Problem(ex.Message);
                    }
                }
            )
            .WithName("ReserveProduct")
            .WithTags("Products")
            .WithDescription("Reserve product quantity");
    }
}
