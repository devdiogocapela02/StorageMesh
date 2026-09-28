using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly StorageMeshDbContext _db;

    public EventsController(StorageMeshDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.NodeEvents
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync();

        return Ok(events);
    }
}
