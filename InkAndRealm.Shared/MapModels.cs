namespace InkAndRealm.Shared;

public sealed class MapDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TreeFeatureDto> Trees { get; set; } = new();
    public List<HouseFeatureDto> Houses { get; set; } = new();
    public List<AreaLayerDto> AreaLayers { get; set; } = new();
    public List<AreaPolygonDto> AreaPolygons { get; set; } = new();
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
    public string? SessionToken { get; set; }
    public int MapId { get; set; }
    public TreeFeatureDto Tree { get; set; } = new();
}

public sealed class AddHouseRequest
{
    public int? UserId { get; set; }
    public string? SessionToken { get; set; }
    public int MapId { get; set; }
    public HouseFeatureDto House { get; set; } = new();
}

public sealed class MapEditsRequest
{
    public int? UserId { get; set; }
    public string? SessionToken { get; set; }
    public int MapId { get; set; }
    public List<TreeFeatureDto> AddedTrees { get; set; } = new();
    public List<HouseFeatureDto> AddedHouses { get; set; } = new();
    public List<AreaPolygonDto> AddedWaterPolygons { get; set; } = new();
    public List<AreaPolygonDto> UpdatedWaterPolygons { get; set; } = new();
}

public sealed class CreateMapRequest
{
    public int? UserId { get; set; }
    public string? SessionToken { get; set; }
}

public sealed class AreaLayerDto
{
    public string LayerKey { get; set; } = string.Empty;
    public int LayerIndex { get; set; }
    public string FeatureType { get; set; } = string.Empty;
}

public sealed class AreaPolygonDto
{
    public int Id { get; set; }
    public string FeatureType { get; set; } = string.Empty;
    public int LayerIndex { get; set; }
    public List<MapPointDto> Points { get; set; } = new();
}

public sealed class MapPointDto
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class MapRenderStateDto
{
    public List<MapPointFeatureDto> PointFeatures { get; set; } = new();
    public List<AreaLayerDto> AreaLayers { get; set; } = new();
    public List<AreaPolygonDto> AreaPolygons { get; set; } = new();
    public AreaPolygonDto? ActivePolygon { get; set; }
    public List<AreaPolygonDto> ActivePolygons { get; set; } = new();
    public AreaPolygonDto? EditPolygon { get; set; }
    public int? EditPolygonPointIndex { get; set; }
    public int? EditPolygonEdgeIndex { get; set; }
    public MapViewStateDto ViewState { get; set; } = new();
}

public sealed class MapPointFeatureDto
{
    public string FeatureType { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public string StyleKey { get; set; } = string.Empty;
    public bool IsStaged { get; set; }
}

public sealed class MapViewStateDto
{
    public float ViewX { get; set; }
    public float ViewY { get; set; }
    public float Zoom { get; set; } = 1f;
    public float MapWidth { get; set; }
    public float MapHeight { get; set; }
}
