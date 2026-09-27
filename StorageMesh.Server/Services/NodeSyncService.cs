using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Models;

namespace StorageMesh.Server.Services;

public class NodeSyncService
{
    private readonly StorageMeshDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NodeSyncService> _logger;

    public NodeSyncService(
        StorageMeshDbContext db,
        IConfiguration configuration, ILogger<NodeSyncService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Sync()
    {

        var ownId = _configuration["Node:Id"];
        var nodes = await _db.KnownNodes.ToListAsync();
        _logger.LogInformation(
    "[{NodeId}] synchronization started",
    ownId);

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

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
                       {     _db.KnownNodes.Add(discovered);
                            _logger.LogInformation(
        "[{NodeId}] DISCOVERED {RemoteNode}",
        ownId,
        discovered.Id);
                        }
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

                    if (string.IsNullOrEmpty(fileKey))
                        continue;

                    var existing = await _db.LocalFiles.FindAsync(fileKey);

                    if (existing != null && File.Exists(existing.StoredAt))
                        continue;

                    var fileResponse =
                        await http.GetAsync($"{node.Url}/api/files/{Uri.EscapeDataString(fileKey)}");

                    if (!fileResponse.IsSuccessStatusCode)
                        continue;

                    var path = Path.Combine("../data", fileKey);

                    await using var stream =
                        new FileStream(path, FileMode.Create, FileAccess.Write);

                    await fileResponse.Content.CopyToAsync(stream);

                    if(existing == null)
                    {
                        _db.LocalFiles.Add(new LocalFile
                        {
                            FileKey = fileKey,
                            StoredAt = path
                        });
                    }
                    else
                    {
                        existing.StoredAt = path;
                    }

                    _db.NodeEvents.Add(new NodeEvent
                    {
                        NodeId=ownId ?? "unknown",
                        EventType = "file_fetched",
                        Detail = $"{fileKey} < {node.Id}",
                        OccurredAt = DateTime.UtcNow
                    });

                    _logger.LogInformation(
    "[{NodeId}] File Fetched {File} from {Source}",
    ownId,
    fileKey,
    node.Id);
                }
            }
            catch
            {
                _logger.LogWarning(
    "[{NodeId}] Node {RemoteNode} is unavailable",
    ownId,
    node.Id);
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation(
    "[{NodeId}] synchronization completed",
    ownId);
    }
}