using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseInMemoryDatabase(
        builder.Configuration.GetConnectionString("DefaultConnectionString")
    )
);

builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(resources => resources.AddService("ProductManagement.Api"))
    .WithTracing(tracing =>
        tracing
            .AddSource("ProductEndpoint")
            .SetSampler(new AlwaysOnSampler())
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4320"); // Collector gRPC endpoint
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            })
    )
    .WithMetrics(metric =>
        metric
            .AddMeter("ProductEndpoint")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317"); // Collector gRPC endpoint
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            })
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapProductEndpoints(app.Services.GetRequiredService<ILoggerFactory>());
app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
}
app.Run();
