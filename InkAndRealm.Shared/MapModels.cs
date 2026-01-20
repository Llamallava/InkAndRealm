namespace InkAndRealm.Shared;

public sealed class MapDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TreeFeatureDto> Trees { get; set; } = new();
    public List<HouseFeatureDto> Houses { get; set; } = new();
}

public sealed class MapSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class TreeFeatureDto
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string TreeType { get; set; } = "Oak";
}

public sealed class HouseFeatureDto
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string HouseType { get; set; } = "Cottage";
}

public sealed class AddTreeRequest
{
    public int? UserId { get; set; }
    public int MapId { get; set; }
    public TreeFeatureDto Tree { get; set; } = new();
}

public sealed class AddHouseRequest
{
    public int? UserId { get; set; }
    public int MapId { get; set; }
    public HouseFeatureDto House { get; set; } = new();
}

public sealed class MapEditsRequest
{
    public int? UserId { get; set; }
    public int MapId { get; set; }
    public List<TreeFeatureDto> AddedTrees { get; set; } = new();
    public List<HouseFeatureDto> AddedHouses { get; set; } = new();
}

public sealed class CreateMapRequest
{
    public int? UserId { get; set; }
}
