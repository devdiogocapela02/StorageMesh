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
        var ownId = _configuration["Node:Id"];
        var nodes = await _db.KnownNodes.ToListAsync();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var result = new List<object>();

        foreach (var node in nodes)
        {
            if (node.Id == ownId) continue;

            var online = false;

            try
            {
                var response = await http.GetAsync($"{node.Url}/health");

                if (response.IsSuccessStatusCode)
                {
                    var health = await response.Content.ReadFromJsonAsync<JsonElement>();
                    online = health.GetProperty("enabled").GetBoolean();
                }
            }
            catch { }

            if (online)
            {
                try
                {
                    var knownResponse = await http.GetAsync($"{node.Url}/api/nodes/known");

                    if (knownResponse.IsSuccessStatusCode)
                    {
                        var knownNodes = await knownResponse.Content.ReadFromJsonAsync<List<KnownNode>>() ?? [];

                        foreach (var discovered in knownNodes)
                        {
                            if (discovered.Id == ownId) continue;

                            var existing = await _db.KnownNodes.FindAsync(discovered.Id);

                            if (existing == null)
                                _db.KnownNodes.Add(discovered);
                            else
                                existing.Url = discovered.Url;
                        }
                    }

                    var filesResponse = await http.GetAsync($"{node.Url}/api/files");

                    if (filesResponse.IsSuccessStatusCode)
                    {
                        var files = await filesResponse.Content.ReadFromJsonAsync<List<JsonElement>>() ?? [];

                        foreach (var remoteFile in files)
                        {
                            var fileKey = remoteFile.GetProperty("fileKey").GetString();

                            if (string.IsNullOrEmpty(fileKey) || await _db.LocalFiles.AnyAsync(f => f.FileKey == fileKey))
                                continue;

                            var fileResponse = await http.GetAsync($"{node.Url}/api/files/{Uri.EscapeDataString(fileKey)}");

                            if (!fileResponse.IsSuccessStatusCode) continue;

                            var path = Path.Combine("../data", fileKey);

                            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                            await fileResponse.Content.CopyToAsync(stream);

                            _db.LocalFiles.Add(new LocalFile { FileKey = fileKey, StoredAt = path });
                        }
                    }
                }
                catch { }
            }

            var previousEvent = await _db.NodeEvents
                .Where(e => e.NodeId == node.Id && (e.EventType == "came_online" || e.EventType == "went_offline"))
                .OrderByDescending(e => e.OccurredAt)
                .FirstOrDefaultAsync();

            var previousOnline = previousEvent?.EventType == "came_online";

            if (previousEvent != null && online != previousOnline)
            {
                _db.NodeEvents.Add(new NodeEvent
                {
                    NodeId = node.Id,
                    EventType = online ? "came_online" : "went_offline",
                    OccurredAt = DateTime.UtcNow
                });
            }

            result.Add(new { id = node.Id, url = node.Url, status = online ? "online" : "offline" });
        }

        await _db.SaveChangesAsync();

        return Ok(result);
    }

    [HttpPost("{id}/off")]
    public async Task<IActionResult> TurnOff(string id)
    {
        var node = await _db.KnownNodes.FirstOrDefaultAsync(node => node.Id == id);

        if (node == null)
            return NotFound();

        using var http = new HttpClient();

        var response = await http.PostAsync(
            $"{node.Url}/api/control/off",
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
        var node = await _db.KnownNodes.FirstOrDefaultAsync(node => node.Id == id);

        if (node == null)
            return NotFound();

        using var http = new HttpClient();

        var response = await http.PostAsync(
            $"{node.Url}/api/control/on",
            null);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        return Ok(new
        {
            id,
            status = "online"
        });
    }

    [HttpGet("known")]
    public async Task<IActionResult> GetKnownNodes()
    {
        return Ok(await _db.KnownNodes.ToListAsync());
    }
}