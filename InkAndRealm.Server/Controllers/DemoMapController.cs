using InkAndRealm.Server.Data;
using InkAndRealm.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace InkAndRealm.Server.Controllers;

[ApiController]
[Route("api/demo-map")]
public sealed class DemoMapController : ControllerBase
{
    private const string DefaultMapName = "Untitled Map";
    private const int TitleNameMaxLength = 128;
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

        return Ok(ToDto(map));
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

        return Ok(ToDto(map));
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

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }

        return Ok(ToDto(map));
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
        return Ok(ToDto(map));
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
        return Ok(ToDto(map));
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

    private static MapDto ToDto(MapEntity map)
    {
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
            return await _context.Sessions
                .Where(session => session.Token == sessionToken
                    && (session.ExpiresUtc == null || session.ExpiresUtc > now))
                .Select(session => session.User)
                .FirstOrDefaultAsync();
        }

        if (userId is null)
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(user => user.Id == userId.Value);
    }
}
