using Microsoft.AspNetCore.Mvc;

namespace StorageMesh.Server.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController: ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                nodeId = _configuration["NodeId"],
                type = _configuration["NodeType"],
                status = "healthy"
            });
        }
    }
}
