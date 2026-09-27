using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Middleware;
using StorageMesh.Server.Models;
using StorageMesh.Server.Services;
using System.Text.Json;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/control")]
public class ControlController : ControllerBase
{
    private readonly StorageMeshDbContext _db;
    private readonly NodeSyncService _sync;
    private readonly ILogger<ControlController> _logger;
    public ControlController(StorageMeshDbContext db, NodeSyncService sync, ILogger<ControlController> logger)
    {
        _db = db;
        _sync= sync;
        _logger = logger;
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

        _logger.LogInformation($"{HttpContext.Items["NodeId"]} recieved start signal");

        NodeMiddleware.Enabled = true;

        
        await _db.SaveChangesAsync();
        await Task.Delay(1000);
        await _sync.Sync();


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

        _logger.LogInformation($"{HttpContext.Items["NodeId"]} recieved shutdown signal");

        NodeMiddleware.Enabled = false;
        
        await _db.SaveChangesAsync();
        await Task.Delay(1000);
        return Ok(new
        {
            status = "offline"
        });
    }
}