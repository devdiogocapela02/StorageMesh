using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Models;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly StorageMeshDbContext _db;
    private readonly IConfiguration _configuration;
    public FilesController(StorageMeshDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var files = await _db.LocalFiles
            .Select(file => new
            {
                file.FileKey,
                file.StoredAt,
                Exists = System.IO.File.Exists(file.StoredAt)
            })
            .ToListAsync();

        return Ok(files);
    }


    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file supplied");
        _db.NodeEvents.Add(new NodeEvent { NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown", EventType = "received_upload", Detail = file.FileName, OccurredAt = DateTime.UtcNow });

        var path = Path.Combine("../data", file.FileName);

        await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream);
        }
        var existing = await _db.LocalFiles.FindAsync(file.FileName);

        if (existing == null)
            _db.LocalFiles.Add(new LocalFile { FileKey = file.FileName, StoredAt = path });

        await _db.SaveChangesAsync();

        var nodes = await _db.KnownNodes.ToListAsync();
        using var http = new HttpClient();

        foreach(var node in nodes)
        {
            try
            {

                using var content = new MultipartFormDataContent();
                using var filestream = System.IO.File.OpenRead(path);
                content.Add(new StreamContent(filestream), "file", file.FileName);
                var response = await http.PostAsync($"{node.Url}/api/files/store", content);
                _db.NodeEvents.Add(new NodeEvent
                {
                    NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                    EventType = response.IsSuccessStatusCode ? "replicated" : "replication_failed",
                    Detail = $"{file.FileName} → {node.Id}",
                    OccurredAt = DateTime.UtcNow
                });
            }
            catch
            {
                _db.NodeEvents.Add(new NodeEvent
                {
                    NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                    EventType = "replication_failed",
                    Detail = $"{file.FileName} → {node.Id}",
                    OccurredAt = DateTime.UtcNow
                });
            }
        }

    await _db.SaveChangesAsync();

        return Ok(new { file = file.FileName });
    }

    [HttpPost("store")]
    public async Task<IActionResult> Store(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file supplied");
        _db.NodeEvents.Add(new NodeEvent { NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown", EventType = "stored_file", Detail = file.FileName, OccurredAt = DateTime.UtcNow });

        var path = Path.Combine("../data", file.FileName);

        await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream);
        }
        var existing = await _db.LocalFiles.FindAsync(file.FileName);

        if (existing == null)
            _db.LocalFiles.Add(new LocalFile { FileKey = file.FileName, StoredAt = path });

        await _db.SaveChangesAsync();

        return Ok(new { file = file.FileName });
    }

    [HttpGet("{fileKey}")]
    public IActionResult GetFile(string fileKey)
    {
        var path = Path.Combine("../data", fileKey);

        if (!System.IO.File.Exists(path)) return NotFound();

        return PhysicalFile(Path.GetFullPath(path), "application/octet-stream", fileKey);
    }
}