using Microsoft.AspNetCore.Mvc;
using StorageMesh.Server.Data;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Models;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/nodes")]
public class NodesController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly StorageMeshDbContext _db;

    public NodesController(IConfiguration configuration, StorageMeshDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetNodes()
    {
        var nodes = _configuration
            .GetSection("Nodes")
            .GetChildren();

        using var http = new HttpClient();

        var result = new List<object>();

        foreach (var node in nodes)
        {
            var id = node["Id"];
            var url = node["Url"];

            var online = false;

            try
            {
                var response = await http.GetAsync($"{url}/health");
                online = response.IsSuccessStatusCode;
            }
            catch
            {
                online = false;
            }

            var previousEvent = await _db.NodeEvents
                .Where(e => e.NodeId == id &&
                            (e.EventType == "came_online" ||
                             e.EventType == "went_offline"))
                .OrderByDescending(e => e.OccurredAt)
                .FirstOrDefaultAsync();
            var previousOnline = previousEvent?.EventType == "came_online";

            if(previousEvent != null && online != previousOnline)
            {
                _db.NodeEvents.Add(new NodeEvent
                {
                    NodeId = id!,
                    EventType = online ? "came_online": "went offline",
                    OccurredAt = DateTime.UtcNow,
                }); 
            }

            result.Add(new
            {
                id,
                url,
                status = online ? "online" : "offline"
            });
        }

        return Ok(result);
    }
}