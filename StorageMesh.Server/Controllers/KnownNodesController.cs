using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/knownnodes")]
public class KnownNodesController : ControllerBase
{
    private readonly StorageMeshDbContext _db;

    public KnownNodesController(StorageMeshDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetKnownNodes()
    {
        return Ok(await _db.KnownNodes.ToListAsync());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKnownNode(string id)
    {
        var node = await _db.KnownNodes.FindAsync(id);

        if (node == null)
            return NotFound();

        _db.KnownNodes.Remove(node);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}