using Microsoft.EntityFrameworkCore;

namespace InkAndRealm.Server.Data;

public sealed class DemoMapContext : DbContext
{
    public DemoMapContext(DbContextOptions<DemoMapContext> options)
        : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<MapEntity> Maps => Set<MapEntity>();
    public DbSet<MapLayerEntity> MapLayers => Set<MapLayerEntity>();
    public DbSet<FeatureEntity> Features => Set<FeatureEntity>();
    public DbSet<FeaturePointEntity> FeaturePoints => Set<FeaturePointEntity>();
    public DbSet<TownStructureEntity> TownStructures => Set<TownStructureEntity>();
    public DbSet<FeatureRelationshipEntity> FeatureRelationships => Set<FeatureRelationshipEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeatureEntity>()
            .HasDiscriminator<string>("FeatureType")
            .HasValue<TitleFeatureEntity>("Title")
            .HasValue<TreeFeatureEntity>("Tree")
            .HasValue<HouseFeatureEntity>("House")
            .HasValue<LandFeatureEntity>("Land")
            .HasValue<WaterFeatureEntity>("Water")
            .HasValue<BridgeFeatureEntity>("Bridge")
            .HasValue<TownFeatureEntity>("Town")
            .HasValue<CharacterFeatureEntity>("Character");

        modelBuilder.Entity<MapEntity>()
            .HasMany(map => map.Features)
            .WithOne(feature => feature.Map)
            .HasForeignKey(feature => feature.MapId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MapEntity>()
            .HasMany(map => map.Layers)
            .WithOne(layer => layer.Map)
            .HasForeignKey(layer => layer.MapId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MapLayerEntity>()
            .HasIndex(layer => new { layer.MapId, layer.LayerIndex })
            .IsUnique();

        modelBuilder.Entity<UserEntity>()
            .HasMany(user => user.Maps)
            .WithOne(map => map.User)
            .HasForeignKey(map => map.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserEntity>()
            .HasMany(user => user.Sessions)
            .WithOne(session => session.User)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FeatureEntity>()
            .HasMany(feature => feature.Points)
            .WithOne(point => point.Feature)
            .HasForeignKey(point => point.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TownFeatureEntity>()
            .HasMany(town => town.Structures)
            .WithOne(structure => structure.Town)
            .HasForeignKey(structure => structure.TownFeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TreeFeatureEntity>()
            .Property(feature => feature.TreeType)
            .HasConversion<string>();

        modelBuilder.Entity<HouseFeatureEntity>()
            .Property(feature => feature.HouseType)
            .HasConversion<string>();

        modelBuilder.Entity<LandFeatureEntity>()
            .Property(feature => feature.LandType)
            .HasConversion<string>();

        modelBuilder.Entity<LandFeatureEntity>()
            .Property(feature => feature.ElevationType)
            .HasConversion<string>();

        modelBuilder.Entity<WaterFeatureEntity>()
            .Property(feature => feature.WaterType)
            .HasConversion<string>();

        modelBuilder.Entity<BridgeFeatureEntity>()
            .Property(feature => feature.BridgeType)
            .HasConversion<string>();

        modelBuilder.Entity<TownFeatureEntity>()
            .Property(feature => feature.TownType)
            .HasConversion<string>();

        modelBuilder.Entity<TownStructureEntity>()
            .Property(structure => structure.TownStructureType)
            .HasConversion<string>();

        modelBuilder.Entity<CharacterFeatureEntity>()
            .Property(character => character.CharacterType)
            .HasConversion<string>();

        modelBuilder.Entity<FeatureRelationshipEntity>()
            .HasOne(relationship => relationship.SourceCharacter)
            .WithMany(character => character.Relationships)
            .HasForeignKey(relationship => relationship.SourceCharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FeatureRelationshipEntity>()
            .HasOne(relationship => relationship.TargetFeature)
            .WithMany()
            .HasForeignKey(relationship => relationship.TargetFeatureId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
