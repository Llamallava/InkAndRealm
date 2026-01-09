using Microsoft.EntityFrameworkCore;

namespace InkAndRealm.Server.Data;

public sealed class DemoMapContext : DbContext
{
    public DemoMapContext(DbContextOptions<DemoMapContext> options)
        : base(options)
    {
    }

    public DbSet<MapEntity> Maps => Set<MapEntity>();
    public DbSet<TreeEntity> Trees => Set<TreeEntity>();
}

public sealed class MapEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TreeEntity> Trees { get; set; } = new();
}

public sealed class TreeEntity
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string TreeType { get; set; } = "Oak";
    public int MapEntityId { get; set; }
    public MapEntity? Map { get; set; }
}
