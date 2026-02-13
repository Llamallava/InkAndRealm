using InkAndRealm.Server.Data;
using InkAndRealm.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace InkAndRealm.Server.Controllers;

[ApiController]
[Route("api/demo-map")]
public sealed class DemoMapController : ControllerBase
{
    private const string DefaultMapName = "Untitled Map";
    private const int TitleNameMaxLength = 128;
    private const int CharacterFieldMaxLength = 512;
    private const float TitleSizeMin = 0.5f;
    private const float TitleSizeMax = 3f;
    private readonly DemoMapContext _context;

    public DemoMapController(DemoMapContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MapSummaryDto>>> GetMaps([FromQuery] int? userId, [FromQuery] string? sessionToken)
    {
        if (userId is null && string.IsNullOrWhiteSpace(sessionToken))
        {
            return Ok(Array.Empty<MapSummaryDto>());
        }

        var user = await ResolveUserAsync(sessionToken, userId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var maps = await _context.Maps
            .Where(map => map.UserId == user.Id)
            .OrderBy(map => map.Id)
            .Select(map => new MapSummaryDto
            {
                Id = map.Id,
                Name = map.Name
            })
            .ToListAsync();

        return Ok(maps);
    }

    [HttpGet("{mapId:int}")]
    public async Task<ActionResult<MapDto>> GetMap([FromRoute] int mapId, [FromQuery] int? userId, [FromQuery] string? sessionToken)
    {
        if (userId is null && string.IsNullOrWhiteSpace(sessionToken))
        {
            return Unauthorized("Log in to load maps.");
        }

        var user = await ResolveUserAsync(sessionToken, userId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .Include(m => m.Layers)
            .FirstOrDefaultAsync(m => m.Id == mapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        var relationships = await LoadRelationshipsAsync(map);
        return Ok(ToDto(map, relationships));
    }

    [HttpPost("new")]
    public async Task<ActionResult<MapDto>> CreateMap([FromBody] CreateMapRequest request)
    {
        if (request is null)
        {
            return BadRequest("Map request is required.");
        }

        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to create a new map.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var mapName = string.IsNullOrWhiteSpace(request.Name)
            ? DefaultMapName
            : request.Name.Trim();

        var map = new MapEntity
        {
            Name = mapName,
            UserId = user.Id
        };

        _context.Maps.Add(map);
        await _context.SaveChangesAsync();

        return Ok(ToDto(map, Array.Empty<FeatureRelationshipEntity>()));
    }

    [HttpPost("edits")]
    public async Task<ActionResult<MapDto>> ApplyEdits([FromBody] MapEditsRequest request)
    {
        if (request is null)
        {
            return BadRequest("Edit payload is required.");
        }

        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to save map changes.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .Include(m => m.Layers)
            .FirstOrDefaultAsync(m => m.Id == request.MapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        var originalFeatureIds = map.Features
            .Select(feature => feature.Id)
            .Where(id => id > 0)
            .ToHashSet();

        var deletedTargetIds = new HashSet<int>();
        CollectDeletedIds(request.DeletedTreeIds, deletedTargetIds);
        CollectDeletedIds(request.DeletedHouseIds, deletedTargetIds);
        CollectDeletedIds(request.DeletedCharacterIds, deletedTargetIds);
        CollectDeletedIds(request.DeletedTitleIds, deletedTargetIds);
        CollectDeletedIds(request.DeletedWaterPolygonIds, deletedTargetIds);
        CollectDeletedIds(request.DeletedLandPolygonIds, deletedTargetIds);

        var hasChanges = false;
        if (request.AreaLayers is not null)
        {
            var normalizedLayers = request.AreaLayers
                .Where(layer => layer is not null)
                .OrderBy(layer => layer.LayerIndex)
                .GroupBy(layer => layer.LayerIndex)
                .Select(group => group.First())
                .ToList();

            map.Layers.Clear();
            foreach (var layer in normalizedLayers)
            {
                map.Layers.Add(new MapLayerEntity
                {
                    LayerKey = layer.LayerKey ?? string.Empty,
                    LayerIndex = layer.LayerIndex,
                    FeatureType = layer.FeatureType ?? string.Empty
                });
            }

            hasChanges = true;
        }
        if (request.AddedTrees is not null && request.AddedTrees.Count > 0)
        {
            foreach (var tree in request.AddedTrees)
            {
                map.Features.Add(CreateTreeFeature(tree));
            }

            hasChanges = true;
        }

        if (request.AddedHouses is not null && request.AddedHouses.Count > 0)
        {
            foreach (var house in request.AddedHouses)
            {
                map.Features.Add(CreateHouseFeature(house));
            }

            hasChanges = true;
        }

        if (request.AddedCharacters is not null && request.AddedCharacters.Count > 0)
        {
            foreach (var character in request.AddedCharacters)
            {
                if (character is null)
                {
                    continue;
                }

                map.Features.Add(CreateCharacterFeature(character));
            }

            hasChanges = true;
        }

        if (request.AddedTitles is not null && request.AddedTitles.Count > 0)
        {
            foreach (var title in request.AddedTitles)
            {
                if (title is null)
                {
                    continue;
                }

                map.Features.Add(CreateTitleFeature(title));
            }

            hasChanges = true;
        }

        if (request.AddedRelationships is not null && request.AddedRelationships.Count > 0)
        {
            var relationshipsChanged = await ApplyAddedRelationshipsAsync(map, request.AddedRelationships, deletedTargetIds);
            hasChanges = hasChanges || relationshipsChanged;
        }

        if (request.UpdatedRelationships is not null && request.UpdatedRelationships.Count > 0)
        {
            var relationshipsChanged = await ApplyUpdatedRelationshipsAsync(map, request.UpdatedRelationships, deletedTargetIds);
            hasChanges = hasChanges || relationshipsChanged;
        }

        if (request.DeletedRelationshipIds is not null && request.DeletedRelationshipIds.Count > 0)
        {
            var relationshipsChanged = await ApplyDeletedRelationshipsAsync(map, request.DeletedRelationshipIds);
            hasChanges = hasChanges || relationshipsChanged;
        }

        if (request.DeletedTreeIds is not null && request.DeletedTreeIds.Count > 0)
        {
            var deletedSet = request.DeletedTreeIds
                .Where(id => id > 0)
                .ToHashSet();
            if (deletedSet.Count > 0)
            {
                var featuresToRemove = map.Features
                    .OfType<TreeFeatureEntity>()
                    .Where(feature => deletedSet.Contains(feature.Id))
                    .ToList();
                foreach (var feature in featuresToRemove)
                {
                    map.Features.Remove(feature);
                }

                if (featuresToRemove.Count > 0)
                {
                    var titleRemovals = map.Features
                        .OfType<TitleFeatureEntity>()
                        .Where(title => title.TargetFeatureId.HasValue
                            && deletedSet.Contains(title.TargetFeatureId.Value))
                        .ToList();
                    foreach (var title in titleRemovals)
                    {
                        map.Features.Remove(title);
                    }

                    hasChanges = true;
                }
                else
                {
                    hasChanges = hasChanges || featuresToRemove.Count > 0;
                }
            }
        }

        if (request.DeletedHouseIds is not null && request.DeletedHouseIds.Count > 0)
        {
            var deletedSet = request.DeletedHouseIds
                .Where(id => id > 0)
                .ToHashSet();
            if (deletedSet.Count > 0)
            {
                var featuresToRemove = map.Features
                    .OfType<HouseFeatureEntity>()
                    .Where(feature => deletedSet.Contains(feature.Id))
                    .ToList();
                foreach (var feature in featuresToRemove)
                {
                    map.Features.Remove(feature);
                }

                if (featuresToRemove.Count > 0)
                {
                    var titleRemovals = map.Features
                        .OfType<TitleFeatureEntity>()
                        .Where(title => title.TargetFeatureId.HasValue
                            && deletedSet.Contains(title.TargetFeatureId.Value))
                        .ToList();
                    foreach (var title in titleRemovals)
                    {
                        map.Features.Remove(title);
                    }

                    hasChanges = true;
                }
                else
                {
                    hasChanges = hasChanges || featuresToRemove.Count > 0;
                }
            }
        }

        if (request.DeletedCharacterIds is not null && request.DeletedCharacterIds.Count > 0)
        {
            var deletedSet = request.DeletedCharacterIds
                .Where(id => id > 0)
                .ToHashSet();
            if (deletedSet.Count > 0)
            {
                var featuresToRemove = map.Features
                    .OfType<CharacterFeatureEntity>()
                    .Where(feature => deletedSet.Contains(feature.Id))
                    .ToList();
                foreach (var feature in featuresToRemove)
                {
                    map.Features.Remove(feature);
                }

                hasChanges = hasChanges || featuresToRemove.Count > 0;
            }
        }

        if (request.DeletedTitleIds is not null && request.DeletedTitleIds.Count > 0)
        {
            var deletedSet = request.DeletedTitleIds
                .Where(id => id > 0)
                .ToHashSet();
            if (deletedSet.Count > 0)
            {
                var featuresToRemove = map.Features
                    .OfType<TitleFeatureEntity>()
                    .Where(feature => deletedSet.Contains(feature.Id))
                    .ToList();
                foreach (var feature in featuresToRemove)
                {
                    map.Features.Remove(feature);
                }

                hasChanges = hasChanges || featuresToRemove.Count > 0;
            }
        }

        if (request.AddedWaterPolygons is not null && request.AddedWaterPolygons.Count > 0)
        {
            foreach (var polygon in request.AddedWaterPolygons)
            {
                map.Features.Add(CreateWaterFeature(polygon));
            }

            hasChanges = true;
        }

        if (request.AddedLandPolygons is not null && request.AddedLandPolygons.Count > 0)
        {
            foreach (var polygon in request.AddedLandPolygons)
            {
                map.Features.Add(CreateLandFeature(polygon));
            }

            hasChanges = true;
        }

        if (request.DeletedWaterPolygonIds is not null && request.DeletedWaterPolygonIds.Count > 0)
        {
            var deletedSet = request.DeletedWaterPolygonIds
                .Where(id => id > 0)
                .ToHashSet();
            if (deletedSet.Count > 0)
            {
                var featuresToRemove = map.Features
                    .OfType<WaterFeatureEntity>()
                    .Where(feature => deletedSet.Contains(feature.Id))
                    .ToList();
                foreach (var feature in featuresToRemove)
                {
                    map.Features.Remove(feature);
                }

                hasChanges = hasChanges || featuresToRemove.Count > 0;
            }
        }

        if (request.DeletedLandPolygonIds is not null && request.DeletedLandPolygonIds.Count > 0)
        {
            var deletedSet = request.DeletedLandPolygonIds
                .Where(id => id > 0)
                .ToHashSet();
            if (deletedSet.Count > 0)
            {
                var featuresToRemove = map.Features
                    .OfType<LandFeatureEntity>()
                    .Where(feature => deletedSet.Contains(feature.Id))
                    .ToList();
                foreach (var feature in featuresToRemove)
                {
                    map.Features.Remove(feature);
                }

                hasChanges = hasChanges || featuresToRemove.Count > 0;
            }
        }

        if (request.UpdatedTrees is not null && request.UpdatedTrees.Count > 0)
        {
            foreach (var tree in request.UpdatedTrees)
            {
                if (tree is null || tree.Id <= 0)
                {
                    continue;
                }

                var feature = map.Features
                    .OfType<TreeFeatureEntity>()
                    .FirstOrDefault(existing => existing.Id == tree.Id);
                if (feature is null)
                {
                    continue;
                }

                feature.TreeType = Enum.TryParse<TreeType>(tree.TreeType, out var treeType) ? treeType : TreeType.Oak;
                feature.ZIndex = tree.LayerIndex;
                feature.Size = NormalizePointSize(tree.Size);
                feature.Points.Clear();
                feature.Points.Add(new FeaturePointEntity
                {
                    X = tree.X,
                    Y = tree.Y,
                    SortOrder = 0
                });

                hasChanges = true;
            }
        }

        if (request.UpdatedHouses is not null && request.UpdatedHouses.Count > 0)
        {
            foreach (var house in request.UpdatedHouses)
            {
                if (house is null || house.Id <= 0)
                {
                    continue;
                }

                var feature = map.Features
                    .OfType<HouseFeatureEntity>()
                    .FirstOrDefault(existing => existing.Id == house.Id);
                if (feature is null)
                {
                    continue;
                }

                feature.HouseType = Enum.TryParse<HouseType>(house.HouseType, out var houseType) ? houseType : HouseType.Cottage;
                feature.ZIndex = house.LayerIndex;
                feature.Size = NormalizePointSize(house.Size);
                feature.Points.Clear();
                feature.Points.Add(new FeaturePointEntity
                {
                    X = house.X,
                    Y = house.Y,
                    SortOrder = 0
                });

                hasChanges = true;
            }
        }

        if (request.UpdatedCharacters is not null && request.UpdatedCharacters.Count > 0)
        {
            foreach (var character in request.UpdatedCharacters)
            {
                if (character is null || character.Id <= 0)
                {
                    continue;
                }

                var feature = map.Features
                    .OfType<CharacterFeatureEntity>()
                    .FirstOrDefault(existing => existing.Id == character.Id);
                if (feature is null)
                {
                    continue;
                }

                feature.CharacterType = Enum.TryParse<CharacterType>(character.CharacterType, out var characterType)
                    ? characterType
                    : CharacterType.Commoner;
                feature.Name = NormalizeCharacterText(character.Name);
                feature.Background = NormalizeCharacterText(character.Background);
                feature.Occupation = NormalizeCharacterText(character.Occupation);
                feature.Personality = NormalizeCharacterText(character.Personality);
                feature.ZIndex = character.LayerIndex;
                feature.Points.Clear();
                feature.Points.Add(new FeaturePointEntity
                {
                    X = character.X,
                    Y = character.Y,
                    SortOrder = 0
                });

                hasChanges = true;
            }
        }

        if (request.UpdatedTitles is not null && request.UpdatedTitles.Count > 0)
        {
            foreach (var title in request.UpdatedTitles)
            {
                if (title is null || title.Id <= 0)
                {
                    continue;
                }

                var feature = map.Features
                    .OfType<TitleFeatureEntity>()
                    .FirstOrDefault(existing => existing.Id == title.Id);
                if (feature is null)
                {
                    continue;
                }

                feature.Name = NormalizeTitleName(title.Name);
                feature.Description = string.IsNullOrWhiteSpace(title.Description) ? null : title.Description.Trim();
                feature.Size = NormalizeTitleSize(title.Size);

                if (!feature.TargetFeatureId.HasValue)
                {
                    feature.Points.Clear();
                    if (title.Points is not null && title.Points.Count >= 3)
                    {
                        for (var i = 0; i < title.Points.Count; i += 1)
                        {
                            var point = title.Points[i];
                            feature.Points.Add(new FeaturePointEntity
                            {
                                X = point.X,
                                Y = point.Y,
                                SortOrder = i
                            });
                        }
                    }
                    else
                    {
                        feature.Points.Add(new FeaturePointEntity
                        {
                            X = title.X,
                            Y = title.Y,
                            SortOrder = 0
                        });
                    }
                }

                hasChanges = true;
            }
        }

        if (request.UpdatedWaterPolygons is not null && request.UpdatedWaterPolygons.Count > 0)
        {
            foreach (var polygon in request.UpdatedWaterPolygons)
            {
                if (polygon is null || polygon.Id <= 0)
                {
                    continue;
                }

                var feature = map.Features
                    .OfType<WaterFeatureEntity>()
                    .FirstOrDefault(existing => existing.Id == polygon.Id);
                if (feature is null)
                {
                    continue;
                }

                feature.ZIndex = polygon.LayerIndex;
                feature.Points.Clear();
                if (polygon.Points is not null)
                {
                    for (var i = 0; i < polygon.Points.Count; i += 1)
                    {
                        var point = polygon.Points[i];
                        feature.Points.Add(new FeaturePointEntity
                        {
                            X = point.X,
                            Y = point.Y,
                            SortOrder = i
                        });
                    }
                }

                hasChanges = true;
            }
        }

        if (request.UpdatedLandPolygons is not null && request.UpdatedLandPolygons.Count > 0)
        {
            foreach (var polygon in request.UpdatedLandPolygons)
            {
                if (polygon is null || polygon.Id <= 0)
                {
                    continue;
                }

                var feature = map.Features
                    .OfType<LandFeatureEntity>()
                    .FirstOrDefault(existing => existing.Id == polygon.Id);
                if (feature is null)
                {
                    continue;
                }

                feature.ZIndex = polygon.LayerIndex;
                feature.Points.Clear();
                if (polygon.Points is not null)
                {
                    for (var i = 0; i < polygon.Points.Count; i += 1)
                    {
                        var point = polygon.Points[i];
                        feature.Points.Add(new FeaturePointEntity
                        {
                            X = point.X,
                            Y = point.Y,
                            SortOrder = i
                        });
                    }
                }

                hasChanges = true;
            }
        }

        if (deletedTargetIds.Count > 0)
        {
            var mapFeatureIds = map.Features
                .Select(feature => feature.Id)
                .Where(id => id > 0)
                .ToHashSet();
            var removalTargets = deletedTargetIds
                .Where(id => originalFeatureIds.Contains(id) && !mapFeatureIds.Contains(id))
                .ToHashSet();

            if (removalTargets.Count > 0)
            {
                var removedRelationships = await RemoveRelationshipsForTargetsAsync(removalTargets);
                hasChanges = hasChanges || removedRelationships;
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }

        var relationships = await LoadRelationshipsAsync(map);
        return Ok(ToDto(map, relationships));
    }

    [HttpPost("tree")]
    public async Task<ActionResult<MapDto>> AddTree([FromBody] AddTreeRequest request)
    {
        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to save map changes.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        if (request.Tree is null)
        {
            return BadRequest("Tree data is required.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .FirstOrDefaultAsync(m => m.Id == request.MapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        map.Features.Add(CreateTreeFeature(request.Tree));

        await _context.SaveChangesAsync();
        var relationships = await LoadRelationshipsAsync(map);
        return Ok(ToDto(map, relationships));
    }

    [HttpPost("house")]
    public async Task<ActionResult<MapDto>> AddHouse([FromBody] AddHouseRequest request)
    {
        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to save map changes.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        if (request.House is null)
        {
            return BadRequest("House data is required.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .FirstOrDefaultAsync(m => m.Id == request.MapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        map.Features.Add(CreateHouseFeature(request.House));

        await _context.SaveChangesAsync();
        var relationships = await LoadRelationshipsAsync(map);
        return Ok(ToDto(map, relationships));
    }

    private static TreeFeatureEntity CreateTreeFeature(TreeFeatureDto tree)
    {
        return new TreeFeatureEntity
        {
            TreeType = Enum.TryParse<TreeType>(tree.TreeType, out var treeType) ? treeType : TreeType.Oak,
            ZIndex = tree.LayerIndex,
            Size = NormalizePointSize(tree.Size),
            Points =
            {
                new FeaturePointEntity
                {
                    X = tree.X,
                    Y = tree.Y,
                    SortOrder = 0
                }
            }
        };
    }

    private static HouseFeatureEntity CreateHouseFeature(HouseFeatureDto house)
    {
        return new HouseFeatureEntity
        {
            HouseType = Enum.TryParse<HouseType>(house.HouseType, out var houseType) ? houseType : HouseType.Cottage,
            ZIndex = house.LayerIndex,
            Size = NormalizePointSize(house.Size),
            Points =
            {
                new FeaturePointEntity
                {
                    X = house.X,
                    Y = house.Y,
                    SortOrder = 0
                }
            }
        };
    }

    private static CharacterFeatureEntity CreateCharacterFeature(CharacterFeatureDto character)
    {
        return new CharacterFeatureEntity
        {
            CharacterType = Enum.TryParse<CharacterType>(character.CharacterType, out var characterType)
                ? characterType
                : CharacterType.Commoner,
            Name = NormalizeCharacterText(character.Name),
            Background = NormalizeCharacterText(character.Background),
            Occupation = NormalizeCharacterText(character.Occupation),
            Personality = NormalizeCharacterText(character.Personality),
            ZIndex = character.LayerIndex,
            Points =
            {
                new FeaturePointEntity
                {
                    X = character.X,
                    Y = character.Y,
                    SortOrder = 0
                }
            }
        };
    }

    private static TitleFeatureEntity CreateTitleFeature(TitleFeatureDto title)
    {
        var name = NormalizeTitleName(title.Name);
        var description = string.IsNullOrWhiteSpace(title.Description) ? null : title.Description.Trim();

        var targetId = title.TargetFeatureId.HasValue && title.TargetFeatureId.Value > 0
            ? title.TargetFeatureId
            : null;

        var feature = new TitleFeatureEntity
        {
            Name = name,
            Description = description,
            TargetFeatureId = targetId,
            Size = NormalizeTitleSize(title.Size),
            ZIndex = 0
        };

        if (title.Points is not null && title.Points.Count >= 3)
        {
            for (var i = 0; i < title.Points.Count; i += 1)
            {
                var point = title.Points[i];
                feature.Points.Add(new FeaturePointEntity
                {
                    X = point.X,
                    Y = point.Y,
                    SortOrder = i
                });
            }
        }
        else
        {
            feature.Points.Add(new FeaturePointEntity
            {
                X = title.X,
                Y = title.Y,
                SortOrder = 0
            });
        }

        return feature;
    }

    private static WaterFeatureEntity CreateWaterFeature(AreaPolygonDto polygon)
    {
        var feature = new WaterFeatureEntity
        {
            WaterType = WaterType.Lake,
            ZIndex = polygon?.LayerIndex ?? 0
        };

        if (polygon?.Points is not null)
        {
            for (var i = 0; i < polygon.Points.Count; i += 1)
            {
                var point = polygon.Points[i];
                feature.Points.Add(new FeaturePointEntity
                {
                    X = point.X,
                    Y = point.Y,
                    SortOrder = i
                });
            }
        }

        return feature;
    }

    private static LandFeatureEntity CreateLandFeature(AreaPolygonDto polygon)
    {
        var feature = new LandFeatureEntity
        {
            LandType = LandType.Plains,
            ElevationType = ElevationType.Low,
            ZIndex = polygon?.LayerIndex ?? 0
        };

        if (polygon?.Points is not null)
        {
            for (var i = 0; i < polygon.Points.Count; i += 1)
            {
                var point = polygon.Points[i];
                feature.Points.Add(new FeaturePointEntity
                {
                    X = point.X,
                    Y = point.Y,
                    SortOrder = i
                });
            }
        }

        return feature;
    }

    private static MapDto ToDto(MapEntity map, IReadOnlyList<FeatureRelationshipEntity>? relationships)
    {
        var featureLookup = map.Features
            .Where(feature => feature.Id > 0)
            .ToDictionary(feature => feature.Id);

        var waterPolygons = map.Features
            .OfType<WaterFeatureEntity>()
            .Select(feature => new AreaPolygonDto
            {
                Id = feature.Id,
                FeatureType = "Water",
                LayerIndex = feature.ZIndex,
                Points = feature.Points
                    .OrderBy(point => point.SortOrder)
                    .Select(point => new MapPointDto
                    {
                        X = point.X,
                        Y = point.Y
                    })
                    .ToList()
            })
            .ToList();

        var landPolygons = map.Features
            .OfType<LandFeatureEntity>()
            .Select(feature => new AreaPolygonDto
            {
                Id = feature.Id,
                FeatureType = "Land",
                LayerIndex = feature.ZIndex,
                Points = feature.Points
                    .OrderBy(point => point.SortOrder)
                    .Select(point => new MapPointDto
                    {
                        X = point.X,
                        Y = point.Y
                    })
                    .ToList()
            })
            .ToList();

        var areaPolygons = waterPolygons.Concat(landPolygons).ToList();

        var areaLayers = areaPolygons
            .GroupBy(polygon => new { polygon.FeatureType, polygon.LayerIndex })
            .OrderBy(group => group.Key.LayerIndex)
            .Select(group => new AreaLayerDto
            {
                LayerKey = $"{group.Key.FeatureType?.ToLowerInvariant() ?? "area"}-{group.Key.LayerIndex}",
                LayerIndex = group.Key.LayerIndex,
                FeatureType = group.Key.FeatureType ?? string.Empty
            })
            .ToList();

        var persistedLayers = map.Layers is null || map.Layers.Count == 0
            ? areaLayers
            : map.Layers
                .OrderBy(layer => layer.LayerIndex)
                .Select(layer => new AreaLayerDto
                {
                    LayerKey = layer.LayerKey,
                    LayerIndex = layer.LayerIndex,
                    FeatureType = layer.FeatureType
                })
                .ToList();

        var titles = map.Features
            .OfType<TitleFeatureEntity>()
            .Select(title =>
            {
                var orderedPoints = title.Points
                    .OrderBy(p => p.SortOrder)
                    .ToList();
                var point = orderedPoints.FirstOrDefault();
                var targetPoint = title.TargetFeatureId.HasValue
                    ? map.Features.FirstOrDefault(feature => feature.Id == title.TargetFeatureId.Value)?
                        .Points.OrderBy(p => p.SortOrder).FirstOrDefault()
                    : null;
                var anchor = targetPoint ?? (orderedPoints.Count >= 3
                    ? GetPolygonCentroid(orderedPoints)
                    : point);
                var mappedPoints = orderedPoints
                    .Select(p => new MapPointDto { X = p.X, Y = p.Y })
                    .ToList();

                return new TitleFeatureDto
                {
                    Id = title.Id,
                    Name = title.Name,
                    Description = title.Description,
                    TargetFeatureId = title.TargetFeatureId,
                    Size = NormalizeTitleSize(title.Size),
                    X = anchor?.X ?? 0f,
                    Y = anchor?.Y ?? 0f,
                    Points = mappedPoints
                };
            })
            .ToList();

        var relationshipsBySource = relationships is null
            ? new Dictionary<int, List<FeatureRelationshipEntity>>()
            : relationships
                .GroupBy(relationship => relationship.SourceCharacterId)
                .ToDictionary(group => group.Key, group => group.OrderBy(r => r.Id).ToList());

        return new MapDto
        {
            Id = map.Id,
            Name = map.Name,
            Trees = map.Features
                .OfType<TreeFeatureEntity>()
                .Select(tree =>
                {
                    var point = tree.Points.OrderBy(p => p.SortOrder).FirstOrDefault();
                    return new TreeFeatureDto
                    {
                        Id = tree.Id,
                        X = point?.X ?? 0f,
                        Y = point?.Y ?? 0f,
                        TreeType = tree.TreeType.ToString(),
                        LayerIndex = tree.ZIndex,
                        Size = NormalizePointSize(tree.Size)
                    };
                })
                .ToList(),
            Houses = map.Features
                .OfType<HouseFeatureEntity>()
                .Select(house =>
                {
                    var point = house.Points.OrderBy(p => p.SortOrder).FirstOrDefault();
                    return new HouseFeatureDto
                    {
                        Id = house.Id,
                        X = point?.X ?? 0f,
                        Y = point?.Y ?? 0f,
                        HouseType = house.HouseType.ToString(),
                        LayerIndex = house.ZIndex,
                        Size = NormalizePointSize(house.Size)
                    };
                })
                .ToList(),
            Characters = map.Features
                .OfType<CharacterFeatureEntity>()
                .Select(character =>
                {
                    var point = character.Points.OrderBy(p => p.SortOrder).FirstOrDefault();
                    relationshipsBySource.TryGetValue(character.Id, out var characterRelationships);
                    var mappedRelationships = characterRelationships is null
                        ? new List<CharacterRelationshipDto>()
                        : characterRelationships
                            .Select(relationship =>
                            {
                                if (!featureLookup.TryGetValue(relationship.TargetFeatureId, out var targetFeature))
                                {
                                    return null;
                                }

                                var targetType = GetFeatureTypeKey(targetFeature);
                                if (targetType == "Title")
                                {
                                    return null;
                                }

                                return new CharacterRelationshipDto
                                {
                                    Id = relationship.Id,
                                    TargetFeatureId = relationship.TargetFeatureId,
                                    TargetFeatureType = targetType,
                                    RelationshipTypes = DeserializeRelationshipTypes(relationship.RelationshipType),
                                    Description = relationship.Description ?? string.Empty
                                };
                            })
                            .Where(relationship => relationship is not null)
                            .Select(relationship => relationship!)
                            .ToList();

                    return new CharacterFeatureDto
                    {
                        Id = character.Id,
                        X = point?.X ?? 0f,
                        Y = point?.Y ?? 0f,
                        CharacterType = character.CharacterType.ToString(),
                        Name = character.Name,
                        Background = character.Background,
                        Occupation = character.Occupation,
                        Personality = character.Personality,
                        LayerIndex = character.ZIndex,
                        Relationships = mappedRelationships
                    };
                })
                .ToList(),
            Titles = titles,
            AreaLayers = persistedLayers,
            AreaPolygons = areaPolygons
        };
    }

    private static string NormalizeTitleName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Untitled";
        }

        var trimmed = name.Trim();
        return trimmed.Length <= TitleNameMaxLength ? trimmed : trimmed[..TitleNameMaxLength];
    }

    private static string NormalizeCharacterText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= CharacterFieldMaxLength ? trimmed : trimmed[..CharacterFieldMaxLength];
    }

    private static float NormalizeTitleSize(float size)
    {
        if (!float.IsFinite(size))
        {
            return 1f;
        }

        if (size < TitleSizeMin)
        {
            return TitleSizeMin;
        }

        if (size > TitleSizeMax)
        {
            return TitleSizeMax;
        }

        return size;
    }

    private static FeaturePointEntity? GetPolygonCentroid(IReadOnlyList<FeaturePointEntity> points)
    {
        if (points is null || points.Count == 0)
        {
            return null;
        }

        if (points.Count == 1)
        {
            return points[0];
        }

        float area = 0f;
        float cx = 0f;
        float cy = 0f;

        for (var i = 0; i < points.Count; i += 1)
        {
            var j = (i + 1) % points.Count;
            var p1 = points[i];
            var p2 = points[j];
            var cross = (p1.X * p2.Y) - (p2.X * p1.Y);
            area += cross;
            cx += (p1.X + p2.X) * cross;
            cy += (p1.Y + p2.Y) * cross;
        }

        area *= 0.5f;
        if (MathF.Abs(area) < 0.001f)
        {
            var avgX = points.Average(point => point.X);
            var avgY = points.Average(point => point.Y);
            return new FeaturePointEntity { X = avgX, Y = avgY };
        }

        var factor = 1f / (6f * area);
        return new FeaturePointEntity { X = cx * factor, Y = cy * factor };
    }

    private static float NormalizePointSize(float size)
    {
        if (!float.IsFinite(size) || size <= 0f)
        {
            return 1f;
        }

        return size;
    }

    private async Task<UserEntity?> ResolveUserAsync(string? sessionToken, int? userId)
    {
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            var now = DateTime.UtcNow;
            var session = await _context.Sessions
                .FirstOrDefaultAsync(existing => existing.Token == sessionToken);
            if (session is null)
            {
                return null;
            }

            if (session.ExpiresUtc.HasValue && session.ExpiresUtc.Value <= now)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
                return null;
            }

            var sessionUser = await _context.Users
                .FirstOrDefaultAsync(user => user.Id == session.UserId);
            if (sessionUser is null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
                return null;
            }

            return sessionUser;
        }

        if (userId is null)
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(user => user.Id == userId.Value);
    }

    private static void CollectDeletedIds(List<int>? values, HashSet<int> target)
    {
        if (values is null || values.Count == 0)
        {
            return;
        }

        foreach (var value in values)
        {
            if (value > 0)
            {
                target.Add(value);
            }
        }
    }

    private async Task<List<FeatureRelationshipEntity>> LoadRelationshipsAsync(MapEntity map)
    {
        var characterIds = map.Features
            .OfType<CharacterFeatureEntity>()
            .Select(character => character.Id)
            .Where(id => id > 0)
            .ToList();

        if (characterIds.Count == 0)
        {
            return new List<FeatureRelationshipEntity>();
        }

        var featureIds = map.Features
            .Select(feature => feature.Id)
            .Where(id => id > 0)
            .ToHashSet();

        var relationships = await _context.FeatureRelationships
            .Where(relationship => characterIds.Contains(relationship.SourceCharacterId)
                && featureIds.Contains(relationship.TargetFeatureId))
            .ToListAsync();

        return relationships;
    }

    private async Task<bool> ApplyAddedRelationshipsAsync(
        MapEntity map,
        IReadOnlyList<AddCharacterRelationshipDto> relationships,
        HashSet<int> deletedTargetIds)
    {
        if (relationships is null || relationships.Count == 0)
        {
            return false;
        }

        var characterLookup = map.Features
            .OfType<CharacterFeatureEntity>()
            .Where(character => character.Id > 0)
            .ToDictionary(character => character.Id);

        if (characterLookup.Count == 0)
        {
            return false;
        }

        var featureLookup = map.Features
            .Where(feature => feature.Id > 0)
            .ToDictionary(feature => feature.Id);

        var existingRelationships = await _context.FeatureRelationships
            .Where(relationship => characterLookup.Keys.Contains(relationship.SourceCharacterId))
            .ToListAsync();

        var relationshipLookup = existingRelationships
            .GroupBy(relationship => (relationship.SourceCharacterId, relationship.TargetFeatureId))
            .ToDictionary(group => group.Key, group => group.First());

        var changed = false;

        foreach (var request in relationships)
        {
            if (request is null)
            {
                continue;
            }

            if (request.SourceCharacterId <= 0 || request.TargetFeatureId <= 0)
            {
                continue;
            }

            if (deletedTargetIds.Contains(request.SourceCharacterId) || deletedTargetIds.Contains(request.TargetFeatureId))
            {
                continue;
            }

            if (!characterLookup.ContainsKey(request.SourceCharacterId))
            {
                continue;
            }

            if (!featureLookup.TryGetValue(request.TargetFeatureId, out var targetFeature))
            {
                continue;
            }

            if (targetFeature is TitleFeatureEntity)
            {
                continue;
            }

            var types = NormalizeRelationshipTypes(request.RelationshipTypes);
            if (types.Count == 0)
            {
                continue;
            }

            var description = NormalizeRelationshipDescription(request.Description);
            changed = ApplyRelationshipUpsert(relationshipLookup, request.SourceCharacterId, request.TargetFeatureId, types, description) || changed;

            if (request.CreateReciprocal && targetFeature is CharacterFeatureEntity)
            {
                var reciprocalTypes = NormalizeRelationshipTypes(request.ReciprocalRelationshipTypes ?? types);
                if (reciprocalTypes.Count == 0)
                {
                    continue;
                }

                var reciprocalDescription = string.IsNullOrWhiteSpace(request.ReciprocalDescription)
                    ? description
                    : NormalizeRelationshipDescription(request.ReciprocalDescription);

                changed = ApplyRelationshipUpsert(relationshipLookup, request.TargetFeatureId, request.SourceCharacterId, reciprocalTypes, reciprocalDescription) || changed;
            }
        }

        return changed;
    }

    private async Task<bool> ApplyUpdatedRelationshipsAsync(
        MapEntity map,
        IReadOnlyList<UpdateCharacterRelationshipDto> relationships,
        HashSet<int> deletedTargetIds)
    {
        if (relationships is null || relationships.Count == 0)
        {
            return false;
        }

        var characterIds = map.Features
            .OfType<CharacterFeatureEntity>()
            .Where(character => character.Id > 0)
            .Select(character => character.Id)
            .ToHashSet();

        if (characterIds.Count == 0)
        {
            return false;
        }

        var featureLookup = map.Features
            .Where(feature => feature.Id > 0)
            .ToDictionary(feature => feature.Id);

        var relationshipIds = relationships
            .Where(relationship => relationship is not null)
            .Select(relationship => relationship.Id)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (relationshipIds.Count == 0)
        {
            return false;
        }

        var existingRelationships = await _context.FeatureRelationships
            .Where(relationship => relationshipIds.Contains(relationship.Id))
            .ToDictionaryAsync(relationship => relationship.Id);

        var changed = false;

        foreach (var request in relationships)
        {
            if (request is null)
            {
                continue;
            }

            if (request.Id <= 0 || request.SourceCharacterId <= 0 || request.TargetFeatureId <= 0)
            {
                continue;
            }

            if (deletedTargetIds.Contains(request.SourceCharacterId) || deletedTargetIds.Contains(request.TargetFeatureId))
            {
                continue;
            }

            if (!characterIds.Contains(request.SourceCharacterId))
            {
                continue;
            }

            if (!featureLookup.TryGetValue(request.TargetFeatureId, out var targetFeature))
            {
                continue;
            }

            if (targetFeature is TitleFeatureEntity)
            {
                continue;
            }

            if (!existingRelationships.TryGetValue(request.Id, out var existing))
            {
                continue;
            }

            if (existing.SourceCharacterId != request.SourceCharacterId
                || existing.TargetFeatureId != request.TargetFeatureId)
            {
                continue;
            }

            var types = NormalizeRelationshipTypes(request.RelationshipTypes);
            if (types.Count == 0)
            {
                continue;
            }

            var updatedTypePayload = SerializeRelationshipTypes(types);
            var updatedDescription = NormalizeRelationshipDescription(request.Description);

            if (!string.Equals(existing.RelationshipType, updatedTypePayload, StringComparison.Ordinal))
            {
                existing.RelationshipType = updatedTypePayload;
                changed = true;
            }

            if (!string.Equals(existing.Description, updatedDescription, StringComparison.Ordinal))
            {
                existing.Description = updatedDescription;
                changed = true;
            }
        }

        return changed;
    }

    private async Task<bool> ApplyDeletedRelationshipsAsync(
        MapEntity map,
        IReadOnlyList<int> relationshipIds)
    {
        if (relationshipIds is null || relationshipIds.Count == 0)
        {
            return false;
        }

        var idsToDelete = relationshipIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (idsToDelete.Count == 0)
        {
            return false;
        }

        var characterIds = map.Features
            .OfType<CharacterFeatureEntity>()
            .Where(character => character.Id > 0)
            .Select(character => character.Id)
            .ToHashSet();
        if (characterIds.Count == 0)
        {
            return false;
        }

        var featureIds = map.Features
            .Where(feature => feature.Id > 0)
            .Select(feature => feature.Id)
            .ToHashSet();

        var relationshipsToDelete = await _context.FeatureRelationships
            .Where(relationship =>
                idsToDelete.Contains(relationship.Id)
                && characterIds.Contains(relationship.SourceCharacterId)
                && featureIds.Contains(relationship.TargetFeatureId))
            .ToListAsync();
        if (relationshipsToDelete.Count == 0)
        {
            return false;
        }

        _context.FeatureRelationships.RemoveRange(relationshipsToDelete);
        return true;
    }

    private bool ApplyRelationshipUpsert(
        Dictionary<(int SourceId, int TargetId), FeatureRelationshipEntity> lookup,
        int sourceId,
        int targetId,
        List<string> types,
        string description)
    {
        if (lookup.TryGetValue((sourceId, targetId), out var existing))
        {
            var mergedTypes = NormalizeRelationshipTypes(DeserializeRelationshipTypes(existing.RelationshipType).Concat(types));
            var updatedTypePayload = SerializeRelationshipTypes(mergedTypes);
            var updatedDescription = string.IsNullOrWhiteSpace(description) ? existing.Description : description;

            var changed = false;
            if (!string.Equals(existing.RelationshipType, updatedTypePayload, StringComparison.Ordinal))
            {
                existing.RelationshipType = updatedTypePayload;
                changed = true;
            }

            if (!string.Equals(existing.Description, updatedDescription, StringComparison.Ordinal))
            {
                existing.Description = updatedDescription;
                changed = true;
            }

            return changed;
        }

        var relationship = new FeatureRelationshipEntity
        {
            SourceCharacterId = sourceId,
            TargetFeatureId = targetId,
            RelationshipType = SerializeRelationshipTypes(types),
            Description = description
        };

        _context.FeatureRelationships.Add(relationship);
        lookup[(sourceId, targetId)] = relationship;
        return true;
    }

    private async Task<bool> RemoveRelationshipsForTargetsAsync(HashSet<int> targetIds)
    {
        if (targetIds is null || targetIds.Count == 0)
        {
            return false;
        }

        var relationshipsToRemove = await _context.FeatureRelationships
            .Where(relationship => targetIds.Contains(relationship.TargetFeatureId))
            .ToListAsync();

        if (relationshipsToRemove.Count == 0)
        {
            return false;
        }

        _context.FeatureRelationships.RemoveRange(relationshipsToRemove);
        return true;
    }

    private static List<string> NormalizeRelationshipTypes(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return new List<string>();
        }

        return values
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => value!)
            .ToList();
    }

    private static string NormalizeRelationshipDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
    }

    private static string SerializeRelationshipTypes(IEnumerable<string> values)
    {
        return JsonSerializer.Serialize(NormalizeRelationshipTypes(values));
    }

    private static List<string> DeserializeRelationshipTypes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(trimmed);
                return NormalizeRelationshipTypes(parsed);
            }
            catch (JsonException)
            {
            }
        }

        return NormalizeRelationshipTypes(new[] { trimmed });
    }

    private static string GetFeatureTypeKey(FeatureEntity feature)
    {
        return feature switch
        {
            TreeFeatureEntity => "Tree",
            HouseFeatureEntity => "House",
            LandFeatureEntity => "Land",
            WaterFeatureEntity => "Water",
            BridgeFeatureEntity => "Bridge",
            TownFeatureEntity => "Town",
            CharacterFeatureEntity => "Character",
            TitleFeatureEntity => "Title",
            _ => "Unknown"
        };
    }
}
