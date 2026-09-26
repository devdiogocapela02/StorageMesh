using StorageMesh.Server.Models;

namespace StorageMesh.Server.Middleware;

public class NodeMiddleware
{
    private readonly RequestDelegate _next;

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

        await _next(context);
    }
}
