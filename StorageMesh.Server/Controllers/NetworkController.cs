using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageMesh.Server.Data;
using StorageMesh.Server.Models;

namespace StorageMesh.Server.Controllers
{
    [ApiController]
    [Route("api/network")]
    public class NetworkController : ControllerBase
    {
        private readonly StorageMeshDbContext _db;
        private readonly ILogger<NetworkController> _logger;
        public NetworkController(StorageMeshDbContext db, ILogger<NetworkController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("nodes")]
        public async Task<IActionResult> GetNodes()
        {
            var nodes = await _db.KnownNodes.ToListAsync();
            using var http = new HttpClient();
            var result = new List<object>();
            foreach (var node in nodes)
            {
                try
                {
                    var response = await http.GetAsync($"{node.Url}/health");
                    if (!response.IsSuccessStatusCode)
                        continue;
                    var health = await response.Content.ReadFromJsonAsync<object>();
                    result.Add(new
                    {
                        node.Id,
                        node.Url,
                        health
                    });
                }
                catch
                {
                    _logger.LogWarning($"Node {node.Id} is unavailable.");
                }
            }
            return Ok(result);
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetFiles()
        {
            var nodes = await _db.KnownNodes.ToListAsync();
            using var http = new HttpClient();
            var result = new List<object>();
            foreach (var node in nodes)
            {
                try
                {
                    var response = await http.GetAsync($"{node.Url}/api/files");
                    if (!response.IsSuccessStatusCode) continue;
                    var files = await response.Content.ReadFromJsonAsync<object>();
                    result.Add(new
                    {
                        nodeId = node.Id,
                        files
                    });
                }
                catch
                {
                    _logger.LogWarning($"Node {node.Id} is unavailable.");
                }
            }
            return Ok(result);
        }
        [HttpGet("events")]
        public async Task<IActionResult> GetEvents()
        {
            var nodes = await _db.KnownNodes.ToListAsync();
            using var http = new HttpClient();
            var result = new List<object>();
            foreach (var node in nodes)
            {
                try
                {
                    var response = await http.GetAsync($"{node.Url}/api/events");
                    if (!response.IsSuccessStatusCode) continue;
                    var events = await response.Content.ReadFromJsonAsync<object>();
                    result.Add(new
                    {
                        nodeId = node.Id,
                        events
                    });
                }
                catch
                {
                    _logger.LogWarning($"Node {node.Id} is unavailable.");
                }
            }
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetNetwork()
        {
            var nodes = await _db.KnownNodes.ToListAsync();
            using var http = new HttpClient();
            var result = new List<Object>();
            foreach(var node in nodes)
            {
                try
                {
                    var filesResponse = await http.GetAsync($"{node.Url}/api/files");
                    var eventsResponse = await http.GetAsync($"{node.Url}/api/events");
                    var knownNodesResponse = await http.GetAsync($"{node.Url}/api/knownnodes");
                    var healthResponse = await http.GetAsync($"{node.Url}/health");

                    var files = filesResponse.IsSuccessStatusCode ? await filesResponse.Content.ReadFromJsonAsync<object>() : Array.Empty<object>();
                    var events = eventsResponse.IsSuccessStatusCode ? await eventsResponse.Content.ReadFromJsonAsync<object>() : Array.Empty<object>();
                    var knownNodes = knownNodesResponse.IsSuccessStatusCode ? await knownNodesResponse.Content.ReadFromJsonAsync<object>() : Array.Empty<object>();
                    var health = await healthResponse.Content.ReadFromJsonAsync<object>();

                    result.Add(new
                    {
                        node.Id, health, files, events, knownNodes
                    });
                }
                catch
                {
                    _logger.LogWarning($"Node {node.Id} was unavailable");
                }
            }
            return Ok(result);
        }

    }

}