using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Models;
using StorageMesh.Server.Services;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly StorageMeshDbContext _db;
    private readonly IConfiguration _configuration;

    private readonly NodeSyncService _sync;
    private readonly ILogger<FilesController> _logger;

    private const long MaxFileSize = 5 * 1024 * 1024;
    private const long MaxStorageSize = 200 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = [".txt", ".pdf", ".png", ".jpg", ".jpeg"];

    public FilesController(
        StorageMeshDbContext db,
        IConfiguration configuration,
        ILogger<FilesController> logger,
        NodeSyncService sync
    )
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
        _sync = sync;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var files = await _db
            .LocalFiles.Select(file => new
            {
                file.FileKey,
                file.StoredAt,
                Exists = System.IO.File.Exists(file.StoredAt),
            })
            .ToListAsync();

        return Ok(files);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file supplied");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
            return BadRequest("File type not supported.");

        if (file.Length > MaxFileSize)
            return BadRequest("File exceeds the 5 MB limit.");

        var dataPath = Path.GetFullPath("../data/storage");

        await _sync.Sync();

        var currentSize = Directory
            .GetFiles(dataPath, "*", SearchOption.TopDirectoryOnly)
            .Sum(path => new FileInfo(path).Length);

        if (currentSize + file.Length > MaxStorageSize)
            return BadRequest("Storage limit of 200 MB exceeded.");

        _db.NodeEvents.Add(
            new NodeEvent
            {
                NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                EventType = "received_upload",
                Detail = file.FileName,
                OccurredAt = DateTime.UtcNow,
            }
        );

        var path = Path.Combine("../data/storage", file.FileName);

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

        foreach (var node in nodes)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var filestream = System.IO.File.OpenRead(path);
                content.Add(new StreamContent(filestream), "file", file.FileName);
                var response = await http.PostAsync($"{node.Url}/api/files/store", content);
                _db.NodeEvents.Add(
                    new NodeEvent
                    {
                        NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                        EventType = response.IsSuccessStatusCode
                            ? "replicated"
                            : "replication_failed",
                        Detail = $"{file.FileName} → {node.Id}",
                        OccurredAt = DateTime.UtcNow,
                    }
                );
                _logger.LogInformation(
                    "[{NodeId}] {EventType} {File} to {Target}",
                    HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                    response.IsSuccessStatusCode ? "replicated" : "failed to replicated",
                    file.FileName,
                    node.Id
                );
            }
            catch
            {
                _db.NodeEvents.Add(
                    new NodeEvent
                    {
                        NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                        EventType = "replication_failed",
                        Detail = $"{file.FileName} to {node.Id}",
                        OccurredAt = DateTime.UtcNow,
                    }
                );
                _logger.LogInformation(
                    "[{NodeId}] failed to replicate {File} to {Target}",
                    HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                    file.FileName,
                    node.Id
                );
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { file = file.FileName });
    }

    [HttpPost("store")]
    public async Task<IActionResult> Store(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file supplied");
        _db.NodeEvents.Add(
            new NodeEvent
            {
                NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                EventType = "stored_file",
                Detail = file.FileName,
                OccurredAt = DateTime.UtcNow,
            }
        );

        var path = Path.Combine("../data/storage", file.FileName);

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
        var path = Path.Combine("../data/storage", fileKey);

        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(Path.GetFullPath(path), "application/octet-stream", fileKey);
    }

    [HttpDelete("{fileKey}")]
    public async Task<IActionResult> DeleteFile(string fileKey)
    {
        var file = await _db.LocalFiles.FindAsync(fileKey);

        if (file == null)
            return NotFound();

        if (System.IO.File.Exists(file.StoredAt))
            System.IO.File.Delete(file.StoredAt);

        _db.LocalFiles.Remove(file);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{fileKey}/physical")]
    public async Task<IActionResult> DeletePhysicalFile(string fileKey)
    {
        var file = await _db.LocalFiles.FindAsync(fileKey);

        if (file == null)
            return NotFound();

        if (System.IO.File.Exists(file.StoredAt))
            System.IO.File.Delete(file.StoredAt);

        _db.NodeEvents.Add(
            new NodeEvent
            {
                NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                EventType = "file_deleted",
                Detail = $"{fileKey} (physical)",
                OccurredAt = DateTime.UtcNow,
            }
        );
        _logger.LogInformation($"Physical file {fileKey} removed");

        return Ok(new { file = fileKey, deleted = "physical" });
    }

    [HttpDelete("{fileKey}/entry")]
    public async Task<IActionResult> DeleteFileEntry(string fileKey)
    {
        var file = await _db.LocalFiles.FindAsync(fileKey);

        if (file == null)
            return NotFound();

        _db.LocalFiles.Remove(file);

        await _db.SaveChangesAsync();

        _db.NodeEvents.Add(
            new NodeEvent
            {
                NodeId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown",
                EventType = "file_entry_deleted",
                Detail = fileKey,
                OccurredAt = DateTime.UtcNow,
            }
        );
        _logger.LogInformation($"File {fileKey} removed");

        return Ok(new { file = fileKey, deleted = "entry" });
    }

    [HttpGet("{fileKey}/fetch")]
    public async Task<IActionResult> FetchFile(string fileKey)
    {
        var path = Path.Combine("../data/storage", fileKey);

        if (System.IO.File.Exists(path))
            return PhysicalFile(Path.GetFullPath(path), "application/octet-stream", fileKey);

        var ownId = HttpContext.Items["NodeId"]?.ToString() ?? "unknown";
        var nodes = await _db.KnownNodes.ToListAsync();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        foreach (var node in nodes)
        {
            if (node.Id == ownId)
                continue;

            try
            {
                var response = await http.GetAsync(
                    $"{node.Url}/api/files/{Uri.EscapeDataString(fileKey)}"
                );

                if (!response.IsSuccessStatusCode)
                    continue;

                await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    await response.Content.CopyToAsync(stream);
                }

                var existing = await _db.LocalFiles.FindAsync(fileKey);

                if (existing == null)
                {
                    _db.LocalFiles.Add(new LocalFile { FileKey = fileKey, StoredAt = path });
                }
                else
                {
                    existing.StoredAt = path;
                }

                _db.NodeEvents.Add(
                    new NodeEvent
                    {
                        NodeId = ownId,
                        EventType = "file_fetched",
                        Detail = $"{fileKey} < {node.Id}",
                        OccurredAt = DateTime.UtcNow,
                    }
                );

                _logger.LogInformation(
                    "[{NodeId}] file_fetched {File} from {Source}",
                    ownId,
                    fileKey,
                    node.Id
                );

                await _db.SaveChangesAsync();

                var contentType = "application/octet-stream";

                if (fileKey.EndsWith(".txt"))
                    contentType = "text/plain";
                else if (fileKey.EndsWith(".pdf"))
                    contentType = "application/pdf";
                else if (fileKey.EndsWith(".png"))
                    contentType = "image/png";
                else if (fileKey.EndsWith(".jpg") || fileKey.EndsWith(".jpeg"))
                    contentType = "image/jpeg";

                return PhysicalFile(Path.GetFullPath(path), contentType);
            }
            catch
            {
                // Try the next known node.
            }
        }
        _logger.LogInformation("[{NodeId}] file not found in any known nodes.", ownId);

        return NotFound();
    }
}
