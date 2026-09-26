using StorageMesh.Server.Models;

namespace StorageMesh.Server.Middleware;

public class NodeMiddleware
{
    private readonly RequestDelegate _next;
    public static bool Enabled { get; set; } = true;
    public NodeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IConfiguration configuration)
    {
        context.Items["NodeId"] = configuration["Node:Id"];
        context.Items["NodeType"] = configuration["Node:Type"];
        if (!Enabled && context.Request.Path != "/health" && context.Request.Path != "/api/control/on")
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            await context.Response.WriteAsJsonAsync(new
            {
                status = "offline"
            });

            return;
        }
        await _next(context);
    }
}
