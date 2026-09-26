using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Models;
using System.Text.Json;

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
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                    online = json.GetProperty("enabled").GetBoolean();
                }
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

    [HttpPost("{id}/off")]
    public async Task<IActionResult> TurnOff(string id)
    {
        var node = _configuration
            .GetSection("Nodes")
            .GetChildren()
            .FirstOrDefault(node => node["Id"] == id);

        if (node == null)
            return NotFound();

        using var http = new HttpClient();

        var response = await http.PostAsync(
            $"{node["Url"]}/api/control/off",
            null);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        return Ok(new
        {
            id,
            status = "offline"
        });
    }

    [HttpPost("{id}/on")]
    public async Task<IActionResult> TurnOn(string id)
    {
        var node = _configuration
            .GetSection("Nodes")
            .GetChildren()
            .FirstOrDefault(node => node["Id"] == id);

        if (node == null)
            return NotFound();

        using var http = new HttpClient();

        var response = await http.PostAsync(
            $"{node["Url"]}/api/control/on",
            null);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        return Ok(new
        {
            id,
            status = "online"
        });
    }
}