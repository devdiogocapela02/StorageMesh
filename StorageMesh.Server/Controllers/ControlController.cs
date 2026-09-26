using Microsoft.AspNetCore.Mvc;
using StorageMesh.Server.Data;
using StorageMesh.Server.Middleware;
using StorageMesh.Server.Models;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/control")]
public class ControlController : ControllerBase
{
    private readonly StorageMeshDbContext _db;

    public ControlController(StorageMeshDbContext db)
    {
        _db = db;
    }

    [HttpPost("on")]
    public async Task<IActionResult> TurnOn()
    {
        _db.NodeEvents.Add(new NodeEvent
        {
            NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
            EventType = "received_on",
            OccurredAt = DateTime.UtcNow
        });

        NodeMiddleware.Enabled = true;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            status = "online"
        });
    }

    [HttpPost("off")]
    public async Task<IActionResult> TurnOff()
    {
        _db.NodeEvents.Add(new NodeEvent
        {
            NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
            EventType = "received_off",
            OccurredAt = DateTime.UtcNow
        });

        NodeMiddleware.Enabled = false;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            status = "offline"
        });
    }
}