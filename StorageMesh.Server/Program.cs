var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "node.json", optional: false, reloadOnChange: false);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
