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

        var path = Path.Combine("../data", file.FileName);

        await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream);
        }
        var existing = await _db.LocalFiles.FindAsync(file.FileName);

        if (existing == null)
            _db.LocalFiles.Add(new LocalFile { FileKey = file.FileName, StoredAt = path });

        await _db.SaveChangesAsync();

        var nodes = _configuration.GetSection("Nodes").GetChildren();
        using var http = new HttpClient();

        foreach(var node in nodes)
        {
            using var content = new MultipartFormDataContent();
            using var filestream = System.IO.File.OpenRead(path);
            content.Add(new StreamContent(filestream), "file", file.FileName);

            await http.PostAsync($"{node["Url"]}/api/files/store", content);
        }

        return Ok(new { file = file.FileName });
    }

    [HttpPost("store")]
    public async Task<IActionResult> Store(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file supplied");

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

}