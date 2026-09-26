using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Middleware;
using StorageMesh.Server.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "node.json", optional: true, reloadOnChange: false);

var databasePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "data",
    "storagemesh.db");

builder.Services.AddDbContext<StorageMeshDbContext>(options =>
    options.UseSqlite($"Data Source=../data/storagemesh.db"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Vue", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddControllers();

var app = builder.Build();
app.UseCors("Vue");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StorageMeshDbContext>();
    db.Database.Migrate();
    db.NodeEvents.Add(new NodeEvent
    {
        NodeId = builder.Configuration["Node:Id"] ?? "unknown",
        EventType = "started",
        OccurredAt = DateTime.UtcNow
    });
    var nodes = builder.Configuration.GetSection("Nodes").GetChildren();

    foreach (var node in nodes)
    {
        if (!db.KnownNodes.Any(n => n.Id == node["Id"]))
        {
            db.KnownNodes.Add(new KnownNode
            {
                Id = node["Id"]!,
                Url = node["Url"]!
            });
        }
    }

    db.SaveChanges();
}

app.UseMiddleware<NodeMiddleware>();
app.MapControllers();

app.Run();
