using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly StorageMeshDbContext _db;

    public FilesController(StorageMeshDbContext db)
    {
        _db = db;
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
}