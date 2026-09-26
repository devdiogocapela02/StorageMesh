namespace StorageMesh.Server.Models;

public class NodeEvent
{
    public int Id { get; set; }

    public string NodeId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public DateTime OccurredAt { get; set; }
}
