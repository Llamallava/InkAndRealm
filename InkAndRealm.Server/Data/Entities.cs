using Microsoft.EntityFrameworkCore;

namespace InkAndRealm.Server.Data;

[Index(nameof(UsernameNormalized), IsUnique = true)]
public sealed class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UsernameNormalized { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<MapEntity> Maps { get; set; } = new();
}

public sealed class MapEntity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public UserEntity? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Width { get; set; }
    public float Height { get; set; }
    public List<FeatureEntity> Features { get; set; } = new();
}

public abstract class FeatureEntity
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public MapEntity? Map { get; set; }
    public bool AllowPointPlacement { get; set; }
    public int ZIndex { get; set; }
    public List<FeaturePointEntity> Points { get; set; } = new();
}

public abstract class PointFeatureEntity : FeatureEntity
{
}

public abstract class AreaFeatureEntity : FeatureEntity
{
}

public sealed class TitleFeatureEntity : FeatureEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class TreeFeatureEntity : PointFeatureEntity
{
    public TreeType TreeType { get; set; }
}

public sealed class HouseFeatureEntity : PointFeatureEntity
{
    public HouseType HouseType { get; set; }
}

public sealed class LandFeatureEntity : AreaFeatureEntity
{
    public LandType LandType { get; set; }
    public ElevationType ElevationType { get; set; }
}

public sealed class WaterFeatureEntity : AreaFeatureEntity
{
    public WaterType WaterType { get; set; }
}

public sealed class BridgeFeatureEntity : AreaFeatureEntity
{
    public BridgeType BridgeType { get; set; }
}

public sealed class TownFeatureEntity : AreaFeatureEntity
{
    public TownType TownType { get; set; }
    public List<TownStructureEntity> Structures { get; set; } = new();
}

public sealed class TownStructureEntity
{
    public int Id { get; set; }
    public int TownFeatureId { get; set; }
    public TownFeatureEntity? Town { get; set; }
    public TownStructureType TownStructureType { get; set; }
    public float RelativeX { get; set; }
    public float RelativeY { get; set; }
    public string TextureKey { get; set; } = string.Empty;
}

public sealed class CharacterFeatureEntity : PointFeatureEntity
{
    public CharacterType CharacterType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public List<FeatureRelationshipEntity> Relationships { get; set; } = new();
}

public sealed class FeatureRelationshipEntity
{
    public int Id { get; set; }
    public int SourceCharacterId { get; set; }
    public CharacterFeatureEntity? SourceCharacter { get; set; }
    public int TargetFeatureId { get; set; }
    public FeatureEntity? TargetFeature { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class FeaturePointEntity
{
    public int Id { get; set; }
    public int FeatureId { get; set; }
    public FeatureEntity? Feature { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public int SortOrder { get; set; }
}

public enum TreeType
{
    Oak,
    Pine,
    Birch,
    Palm
}

public enum HouseType
{
    Cottage,
    Cabin,
    Manor
}

public enum LandType
{
    Plains,
    Forest,
    Desert
}

public enum ElevationType
{
    Low,
    Medium,
    High
}

public enum WaterType
{
    Lake,
    River,
    Ocean
}

public enum BridgeType
{
    Stone,
    Wooden,
    Rope
}

public enum TownType
{
    Hamlet,
    Village,
    City
}

public enum TownStructureType
{
    House,
    Market,
    Tavern,
    Temple
}

public enum CharacterType
{
    Commoner,
    Noble,
    Merchant,
    Soldier
}
