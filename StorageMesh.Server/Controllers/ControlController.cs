using Microsoft.AspNetCore.Mvc;
using StorageMesh.Server.Middleware;

namespace StorageMesh.Server.Controllers;

[ApiController]
[Route("api/control")]
public class ControlController : ControllerBase
{
    [HttpPost("on")]
    public IActionResult TurnOn()
    {
        NodeMiddleware.Enabled = true;

        return Ok(new
        {
            status = "online"
        });
    }

    [HttpPost("off")]
    public IActionResult TurnOff()
    {
        NodeMiddleware.Enabled = false;

        return Ok(new
        {
            status = "offline"
        });
    }
}