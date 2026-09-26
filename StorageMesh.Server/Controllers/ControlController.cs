using Microsoft.AspNetCore.Mvc;
using StorageMesh.Server.Data;
using StorageMesh.Server.Middleware;
using StorageMesh.Server.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/control")]
public class ControlController : ControllerBase
{
    private readonly StorageMeshDbContext _db;
    private readonly IConfiguration _configuration; //temporary

    public ControlController(StorageMeshDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration; //temporary
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

        await SyncWithKnownNodes();

        return Ok(new
        {
            status = "online"
        });
    }

    private async Task SyncWithKnownNodes()
    {
        var ownId = _configuration["Node:Id"];
        var nodes = await _db.KnownNodes.ToListAsync();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        foreach (var node in nodes)
        {
            if (node.Id == ownId)
                continue;

            try
            {
                var healthResponse = await http.GetAsync($"{node.Url}/health");

                if (!healthResponse.IsSuccessStatusCode)
                    continue;

                var health = await healthResponse.Content.ReadFromJsonAsync<JsonElement>();

                if (!health.GetProperty("enabled").GetBoolean())
                    continue;

                var knownResponse = await http.GetAsync($"{node.Url}/api/nodes/known");

                if (knownResponse.IsSuccessStatusCode)
                {
                    var knownNodes = await knownResponse.Content.ReadFromJsonAsync<List<KnownNode>>() ?? [];

                    foreach (var discovered in knownNodes)
                    {
                        if (discovered.Id == ownId)
                            continue;

                        var existing = await _db.KnownNodes.FindAsync(discovered.Id);

                        if (existing == null)
                            _db.KnownNodes.Add(discovered);
                        else
                            existing.Url = discovered.Url;
                    }
                }

                var filesResponse = await http.GetAsync($"{node.Url}/api/files");

                if (!filesResponse.IsSuccessStatusCode)
                    continue;

                var files = await filesResponse.Content.ReadFromJsonAsync<List<JsonElement>>() ?? [];

                foreach (var remoteFile in files)
                {
                    var fileKey = remoteFile.GetProperty("fileKey").GetString();

                    if (string.IsNullOrEmpty(fileKey) ||
                        await _db.LocalFiles.AnyAsync(f => f.FileKey == fileKey))
                        continue;

                    var fileResponse =
                        await http.GetAsync($"{node.Url}/api/files/{Uri.EscapeDataString(fileKey)}");

                    if (!fileResponse.IsSuccessStatusCode)
                        continue;

                    var path = Path.Combine("../data", fileKey);

                    await using var stream =
                        new FileStream(path, FileMode.Create, FileAccess.Write);

                    await fileResponse.Content.CopyToAsync(stream);

                    _db.LocalFiles.Add(new LocalFile
                    {
                        FileKey = fileKey,
                        StoredAt = path
                    });
                }
            }
            catch
            {
                // Node may be unavailable.
            }
        }

        await _db.SaveChangesAsync();
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