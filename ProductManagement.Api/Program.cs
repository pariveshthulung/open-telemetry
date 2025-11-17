using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProductManagement.Api.Data;
using ProductManagement.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var inMemorySqlite = new SqliteConnection("Data Source=:memory:");
inMemorySqlite.Open();

builder.Services.AddDbContext<ProductDbContext>(options =>
{
    options.UseSqlite(inMemorySqlite);
});

const string otlpEndpoint = "http://otel-collector:4317";

builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(resources => resources.AddService("ProductManagement.Api"))
    .WithTracing(tracing =>
        tracing
            .AddSource("ProductEndpoint")
            .SetSampler(new AlwaysOnSampler())
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .SetErrorStatusOnException()
            .AddConsoleExporter()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
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
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            })
    );
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;

    options.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri(otlpEndpoint);
        otlp.Protocol = OtlpExportProtocol.Grpc;
    });
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
app.MapProductEndpoints(loggerFactory);
app.MapOtelEndpoints();

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
}
app.Run();
