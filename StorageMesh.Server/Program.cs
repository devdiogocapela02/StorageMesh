using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Middleware;
using StorageMesh.Server.Models;
using StorageMesh.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("node.json", optional: true, reloadOnChange: false);

var nodeType = builder.Configuration["Node:Type"];
var isConsumer = nodeType == "Consumer";

Directory.CreateDirectory("../data/db");
Directory.CreateDirectory("../data/storage");

builder.Services.AddDbContext<StorageMeshDbContext>(options =>
    options.UseSqlite("Data Source=../data/db/storagemesh.db")
);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Vue",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
        }
    );
});

builder.Services.AddControllers();

builder.Services.AddScoped<NodeSyncService>();
builder.Services.AddHostedService<NodeSyncBackgroundService>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.UseCors("Vue");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StorageMeshDbContext>();

    db.Database.Migrate();

    db.NodeEvents.Add(
        new NodeEvent
        {
            NodeId = builder.Configuration["Node:Id"] ?? "unknown",
            EventType = "started",
            OccurredAt = DateTime.UtcNow,
        }
    );

    logger.LogInformation("[{NodeId}] started", builder.Configuration["Node:Id"] ?? "unknown");

    var nodes = builder.Configuration.GetSection("Nodes").GetChildren();

    foreach (var node in nodes)
    {
        if (!db.KnownNodes.Any(n => n.Id == node["Id"]))
        {
            db.KnownNodes.Add(new KnownNode { Id = node["Id"]!, Url = node["Url"]! });
        }
    }

    db.SaveChanges();
}

app.UseRouting();

app.UseMiddleware<NodeMiddleware>();

if (isConsumer)
{
    // Serve Vue assets
    app.MapGet(
        "/assets/{**path}",
        (string path) =>
        {
            var filePath = Path.Combine(app.Environment.WebRootPath!, "assets", path);

            if (!File.Exists(filePath))
                return Results.NotFound();

            var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".css" => "text/css",
                ".js" => "text/javascript",
                ".json" => "application/json",
                ".ico" => "image/x-icon",
                ".svg" => "image/svg+xml",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                _ => "application/octet-stream",
            };

            return Results.File(filePath, contentType);
        }
    );

    // Serve Vue entry point
    app.MapGet(
        "/",
        () =>
        {
            return Results.File(
                Path.Combine(app.Environment.WebRootPath!, "index.html"),
                "text/html"
            );
        }
    );

    // Support Vue client-side routes
    app.MapFallback(() =>
    {
        return Results.File(Path.Combine(app.Environment.WebRootPath!, "index.html"), "text/html");
    });
}

app.MapControllers();

app.Run();
