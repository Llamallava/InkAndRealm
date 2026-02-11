namespace InkAndRealm.Shared;

public sealed class MapDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TreeFeatureDto> Trees { get; set; } = new();
    public List<HouseFeatureDto> Houses { get; set; } = new();
    public List<CharacterFeatureDto> Characters { get; set; } = new();
    public List<TitleFeatureDto> Titles { get; set; } = new();
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
    public int LayerIndex { get; set; }
    public float Size { get; set; } = 1f;
}

public sealed class HouseFeatureDto
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string HouseType { get; set; } = "Cottage";
    public int LayerIndex { get; set; }
    public float Size { get; set; } = 1f;
}

public sealed class CharacterFeatureDto
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string CharacterType { get; set; } = "Commoner";
    public string Name { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public int LayerIndex { get; set; }
    public List<CharacterRelationshipDto> Relationships { get; set; } = new();
}

public sealed class CharacterRelationshipDto
{
    public int Id { get; set; }
    public int TargetFeatureId { get; set; }
    public string TargetFeatureType { get; set; } = string.Empty;
    public List<string> RelationshipTypes { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

public sealed class TitleFeatureDto
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TargetFeatureId { get; set; }
    public float Size { get; set; } = 1f;
    public List<MapPointDto> Points { get; set; } = new();
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
    public List<AreaLayerDto> AreaLayers { get; set; } = new();
    public List<TreeFeatureDto> AddedTrees { get; set; } = new();
    public List<HouseFeatureDto> AddedHouses { get; set; } = new();
    public List<CharacterFeatureDto> AddedCharacters { get; set; } = new();
    public List<TitleFeatureDto> AddedTitles { get; set; } = new();
    public List<TreeFeatureDto> UpdatedTrees { get; set; } = new();
    public List<HouseFeatureDto> UpdatedHouses { get; set; } = new();
    public List<CharacterFeatureDto> UpdatedCharacters { get; set; } = new();
    public List<TitleFeatureDto> UpdatedTitles { get; set; } = new();
    public List<AddCharacterRelationshipDto> AddedRelationships { get; set; } = new();
    public List<UpdateCharacterRelationshipDto> UpdatedRelationships { get; set; } = new();
    public List<int> DeletedRelationshipIds { get; set; } = new();
    public List<int> DeletedTreeIds { get; set; } = new();
    public List<int> DeletedHouseIds { get; set; } = new();
    public List<int> DeletedCharacterIds { get; set; } = new();
    public List<int> DeletedTitleIds { get; set; } = new();
    public List<AreaPolygonDto> AddedWaterPolygons { get; set; } = new();
    public List<int> DeletedWaterPolygonIds { get; set; } = new();
    public List<AreaPolygonDto> UpdatedWaterPolygons { get; set; } = new();
    public List<AreaPolygonDto> AddedLandPolygons { get; set; } = new();
    public List<int> DeletedLandPolygonIds { get; set; } = new();
    public List<AreaPolygonDto> UpdatedLandPolygons { get; set; } = new();
}

public sealed class AddCharacterRelationshipDto
{
    public int SourceCharacterId { get; set; }
    public int TargetFeatureId { get; set; }
    public List<string> RelationshipTypes { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool CreateReciprocal { get; set; }
    public List<string>? ReciprocalRelationshipTypes { get; set; }
    public string? ReciprocalDescription { get; set; }
}

public sealed class UpdateCharacterRelationshipDto
{
    public int Id { get; set; }
    public int SourceCharacterId { get; set; }
    public int TargetFeatureId { get; set; }
    public List<string> RelationshipTypes { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

public sealed class CreateMapRequest
{
    public int? UserId { get; set; }
    public string? SessionToken { get; set; }
    public string? Name { get; set; }
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
    public List<MapTitleFeatureDto> TitleFeatures { get; set; } = new();
    public List<AreaLayerDto> AreaLayers { get; set; } = new();
    public List<AreaPolygonDto> AreaPolygons { get; set; } = new();
    public AreaPolygonDto? ActivePolygon { get; set; }
    public List<AreaPolygonDto> ActivePolygons { get; set; } = new();
    public AreaPolygonDto? EditPolygon { get; set; }
    public MapPointFeatureDto? EditPointFeature { get; set; }
    public int? EditPolygonPointIndex { get; set; }
    public int? EditPolygonEdgeIndex { get; set; }
    public BrushPreviewDto? BrushPreview { get; set; }
    public bool UseChaoticLandEdges { get; set; }
    public MapViewStateDto ViewState { get; set; } = new();
}

public sealed class BrushPreviewDto
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Radius { get; set; }
    public bool IsActive { get; set; }
}

public sealed class MapPointFeatureDto
{
    public string FeatureType { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public string StyleKey { get; set; } = string.Empty;
    public bool IsStaged { get; set; }
    public float Size { get; set; } = 1f;
}

public sealed class MapTitleFeatureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public bool IsStaged { get; set; }
    public float Size { get; set; } = 1f;
    public List<MapPointDto> Points { get; set; } = new();
}

public sealed class MapViewStateDto
{
    public float ViewX { get; set; }
    public float ViewY { get; set; }
    public float Zoom { get; set; } = 1f;
    public float MapWidth { get; set; }
    public float MapHeight { get; set; }
}
