using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
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

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StorageMeshDbContext>();

    if (!db.LocalFiles.Any())
    {
        db.LocalFiles.AddRange(
            new LocalFile
            {
                FileKey = "test-1.txt",
                StoredAt = "../data/test-1.txt"
            },
            new LocalFile
            {
                FileKey = "missing.txt",
                StoredAt = "../data/missing.txt"
            }
        );

        db.SaveChanges();
    }
}

app.MapControllers();

app.Run();
