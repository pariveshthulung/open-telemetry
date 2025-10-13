using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderManagement.Api.Data;
using OrderManagement.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var inMemorySqlite = new SqliteConnection("Data Source=:memory:");
inMemorySqlite.Open();

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlite(inMemorySqlite);
});

builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(resources => resources.AddService("OrderManagement.Api"))
    .WithTracing(tracing =>
        tracing
            .AddSource("ProductEndpoint")
            .SetSampler(new AlwaysOnSampler())
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .SetErrorStatusOnException()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                // options.Endpoint = new Uri("http://localhost:4320"); // Collector gRPC endpoint
                options.Endpoint = new Uri("http://otel-collector:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            })
    )
    .WithMetrics(metric =>
        metric
            .AddMeter("ProductEndpoint")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://otel-collector:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            })
    );

builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapOrderEndpoint();

app.UseHttpsRedirection();

app.Run();
